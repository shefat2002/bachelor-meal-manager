using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MealManager.Api.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace MealManager.IntegrationTests;

public sealed class AuthFlowTests(BmmApiFactory factory) : IClassFixture<BmmApiFactory>
{
    [Fact]
    public async Task Register_Login_Refresh_Rotate_Logout_FullFlow()
    {
        var (client, accessToken, refreshCookie) = await factory.RegisterAsync("+8801700000001", "Alice");

        // Access token works for /me
        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
        var me = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        me.GetProperty("name").GetString().Should().Be("Alice");

        // Login again works and sets a fresh cookie
        var login = await client.PostAsJsonAsync("/api/auth/login", new { phone = "+8801700000001", password = "Passw0rd!" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var cookie1 = BmmApiFactoryExtensions.ExtractRefreshCookie(login);

        client.DefaultRequestHeaders.Authorization = null;

        // Refresh rotates: new access token + new cookie. Fresh clients per stage — the test
        // HttpClient caches cookies per instance (TestServer quirk), which would mask rotation.
        var refresh = await factory.CreateClient().SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh")
        {
            Headers = { { "Cookie", cookie1 } },
        });
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var newAccess = await BmmApiFactoryExtensions.ExtractAccessToken(refresh);
        newAccess.Should().NotBeNullOrWhiteSpace();
        var cookie2 = BmmApiFactoryExtensions.ExtractRefreshCookie(refresh);

        // Replaying the rotated-away cookie triggers reuse detection → 401
        var replay = await factory.CreateClient().SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh")
        {
            Headers = { { "Cookie", cookie1 } },
        });
        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Logout revokes the current refresh token
        var logout = await factory.CreateClient().SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout")
        {
            Headers = { { "Cookie", cookie2 } },
        });
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var afterLogout = await factory.CreateClient().SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh")
        {
            Headers = { { "Cookie", cookie2 } },
        });
        afterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401_Unauthenticated()
    {
        await factory.RegisterAsync("+8801700000002");
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { phone = "+8801700000002", password = "WrongPass1!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("type").GetString().Should().Be("UNAUTHENTICATED");
    }

    [Fact]
    public async Task Register_WithDuplicatePhone_Returns409_Conflict()
    {
        await factory.RegisterAsync("+8801700000003");
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Dup", phone = "+8801700000003", password = "Passw0rd!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("type").GetString().Should().Be("CONFLICT");
    }

    [Fact]
    public async Task Register_WithInvalidPhone_Returns400_WithFieldErrors()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Bad", phone = "12", password = "Passw0rd!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("type").GetString().Should().Be("VALIDATION_ERROR");
        body.GetProperty("errors").EnumerateObject().Should().Contain(p => p.Name == "phone");
    }

    [Fact]
    public async Task Otp_RequestAndVerify_ReturnsTokens()
    {
        await factory.RegisterAsync("+8801700000004");
        var client = factory.CreateClient();

        // Development environment returns the code for testing (no SMS until Phase 4)
        var request = await client.PostAsJsonAsync("/api/auth/otp/request", new { phone = "+8801700000004" });
        request.EnsureSuccessStatusCode();
        var code = (await request.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("devCode").GetString()!;

        var verify = await client.PostAsJsonAsync("/api/auth/otp/verify", new { phone = "+8801700000004", code });
        verify.EnsureSuccessStatusCode();
        var accessToken = await BmmApiFactoryExtensions.ExtractAccessToken(verify);
        accessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
