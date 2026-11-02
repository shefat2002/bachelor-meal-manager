using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace MealManager.IntegrationTests;

/// <summary>Mess CRUD + invite/join/approve lifecycle + tenant-header enforcement (X-Mess-Id contract).</summary>
public sealed class MessMemberFlowTests(BmmApiFactory factory) : IClassFixture<BmmApiFactory>
{
    [Fact]
    public async Task Full_Mess_Lifecycle_WithTenantRules()
    {
        var (alice, aliceToken, _) = await factory.RegisterAsync("+8801710000001", "Alice");
        var (bob, bobToken, _) = await factory.RegisterAsync("+8801710000002", "Bob");
        alice.DefaultRequestHeaders.Authorization = new("Bearer", aliceToken);
        bob.DefaultRequestHeaders.Authorization = new("Bearer", bobToken);

        // Alice creates a mess → creator becomes MealManager
        var create = await alice.PostAsJsonAsync("/api/messes", new { name = "Station Mess" });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        create.Headers.Location.Should().NotBeNull();
        var mess = await create.Content.ReadFromJsonAsync<JsonElement>();
        var messId = mess.GetProperty("id").GetString()!;
        mess.GetProperty("myRole").GetString().Should().Be("MealManager");

        // Mess list (paginated envelope) contains it
        var list = await alice.GetFromJsonAsync<JsonElement>("/api/messes?page=1&pageSize=10");
        list.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        list.GetProperty("items").EnumerateArray().Should().Contain(m => m.GetProperty("id").GetString() == messId);

        // Bob cannot see Alice's mess (404 masks cross-tenant existence)
        var bobSees = await bob.GetAsync($"/api/messes/{messId}");
        bobSees.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Invite is tenant-scoped: manager sends X-Mess-Id; Bob joins → pending
        var invite = await alice.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/members/invite")
        {
            Headers = { { "X-Mess-Id", messId } },
        });
        invite.EnsureSuccessStatusCode();
        var inviteToken = (await invite.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("inviteToken").GetString()!;

        var join = await bob.PostAsJsonAsync("/api/members/join", new { inviteToken });
        join.StatusCode.Should().Be(HttpStatusCode.Created);

        // Double join → 409
        var joinAgain = await bob.PostAsJsonAsync("/api/members/join", new { inviteToken });
        joinAgain.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Members list without X-Mess-Id → 400 TENANT_REQUIRED
        var noHeader = await alice.GetAsync("/api/members");
        noHeader.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await noHeader.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("type").GetString().Should().Be("TENANT_REQUIRED");

        // Bob (pending member) is not active yet → 403 on tenant-scoped calls
        var bobForbiddenResponse = await bob.SendAsync(NewMemberListRequest(bob, messId));
        bobForbiddenResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Alice (manager, via X-Mess-Id) sees the pending member and approves
        var pending = await alice.SendAsync(NewMemberListRequest(alice, messId));
        pending.EnsureSuccessStatusCode();
        var pendingBody = await pending.Content.ReadFromJsonAsync<JsonElement>();
        var bobMemberId = pendingBody.GetProperty("items").EnumerateArray()
            .Single(m => m.GetProperty("name").GetString() == "Bob")
            .GetProperty("memberId").GetString()!;
        pendingBody.GetProperty("items").EnumerateArray()
            .Single(m => m.GetProperty("name").GetString() == "Bob")
            .GetProperty("status").GetString().Should().Be("Pending");

        var approve = await alice.SendAsync(new HttpRequestMessage(HttpMethod.Put, $"/api/members/{bobMemberId}/approve")
        {
            Headers = { { "X-Mess-Id", messId } },
        });
        approve.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Bob is now an active member: members list works via X-Mess-Id, sees only active members
        var bobList = await bob.SendAsync(NewMemberListRequest(bob, messId));
        bobList.EnsureSuccessStatusCode();
        var bobListBody = await bobList.Content.ReadFromJsonAsync<JsonElement>();
        bobListBody.GetProperty("items").EnumerateArray()
            .Should().OnlyContain(m => m.GetProperty("status").GetString() == "Active");

        // Bob (plain member) cannot create invites → 403
        var bobInvite = await bob.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/members/invite")
        {
            Headers = { { "X-Mess-Id", messId } },
        });
        bobInvite.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Alice promotes Bob; with two managers, demoting herself is allowed
        var promote = await alice.SendAsync(new HttpRequestMessage(HttpMethod.Put, $"/api/members/{bobMemberId}/role")
        {
            Content = JsonContent.Create(new { role = "MealManager" }),
            Headers = { { "X-Mess-Id", messId } },
        });
        promote.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var aliceMemberId = pendingBody.GetProperty("items").EnumerateArray()
            .Single(m => m.GetProperty("name").GetString() == "Alice")
            .GetProperty("memberId").GetString()!;
        var demote = await alice.SendAsync(new HttpRequestMessage(HttpMethod.Put, $"/api/members/{aliceMemberId}/role")
        {
            Content = JsonContent.Create(new { role = "Member" }),
            Headers = { { "X-Mess-Id", messId } },
        });
        demote.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Alice deletes her mess (creator)
        var delete = await alice.DeleteAsync($"/api/messes/{messId}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var gone = await alice.GetAsync($"/api/messes/{messId}");
        gone.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TenantHeader_FromNonMember_Returns403()
    {
        var (alice, aliceToken, _) = await factory.RegisterAsync("+8801720000001", "Alice2");
        var (mallory, malloryToken, _) = await factory.RegisterAsync("+8801720000002", "Mallory");
        alice.DefaultRequestHeaders.Authorization = new("Bearer", aliceToken);
        mallory.DefaultRequestHeaders.Authorization = new("Bearer", malloryToken);

        var create = await alice.PostAsJsonAsync("/api/messes", new { name = "Private Mess" });
        var messId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;

        var response = await mallory.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/members")
        {
            Headers = { { "X-Mess-Id", messId } },
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("type").GetString().Should().Be("FORBIDDEN");
    }

    private static HttpRequestMessage NewMemberListRequest(HttpClient client, string messId) =>
        new(HttpMethod.Get, "/api/members") { Headers = { { "X-Mess-Id", messId } } };
}
