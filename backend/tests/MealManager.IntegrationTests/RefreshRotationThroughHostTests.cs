using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MealManager.Application.Abstractions;
using MealManager.Application.Exceptions;
using MealManager.Application.Features.Auth;
using MealManager.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MealManager.IntegrationTests;

/// <summary>Refresh-token rotation proven at the handler level (DB state) and over HTTP (replay must 401).</summary>
public sealed class RefreshRotationThroughHostTests(BmmApiFactory factory) : IClassFixture<BmmApiFactory>
{
    [Fact]
    public async Task Rotation_MarksOldToken_Revoked_And_Replay_Throws401()
    {
        var (_, _, cookie) = await factory.RegisterAsync("+8801740000001");
        var rawToken = cookie["bmm_refresh=".Length..];

        using (var scope = factory.Services.CreateScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var first = await sender.Send(new RefreshTokenCommand(rawToken));
            first.AccessToken.Should().NotBeNullOrWhiteSpace();
        }

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
            var tokenRow = await db.RefreshTokens.AsNoTracking().IgnoreQueryFilters()
                .SingleAsync(r => r.Hash == new TokenServiceConfigShim(factory).HashOf(rawToken));
            tokenRow.RevokedAt.Should().NotBeNull("the rotated-away token must be revoked");
        }

        using (var scope = factory.Services.CreateScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => sender.Send(new RefreshTokenCommand(rawToken)));
            ex.StatusCode.Should().Be(401);
        }
    }

    [Fact]
    public async Task Rotation_ViaHttp_Minimal()
    {
        // Fresh clients per stage: the test HttpClient caches responses/cookies per instance (TestServer quirk).
        var (_, _, cookie) = await factory.RegisterAsync("+8801740000002");

        var first = await factory.CreateClient().SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh")
        {
            Headers = { { "Cookie", cookie } },
        });
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var replay = await factory.CreateClient().SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh")
        {
            Headers = { { "Cookie", cookie } },
        });
        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed class TokenServiceConfigShim(BmmApiFactory factory)
    {
        private readonly ITokenService _tokens = factory.Services.GetRequiredService<ITokenService>();

        public string HashOf(string token) => _tokens.HashToken(token);
    }
}
