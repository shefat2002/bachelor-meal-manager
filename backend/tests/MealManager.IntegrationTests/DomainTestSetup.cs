using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace MealManager.IntegrationTests;

/// <summary>
/// Shared Phase 2 fixture: a mess (manager) + one approved plain member, both with live tokens.
/// Phones must stay unique within a test class (each class shares one sqlite factory).
/// </summary>
internal static class DomainTestSetup
{
    public static async Task<DomainTestMess> CreateMessWithMemberAsync(BmmApiFactory factory, string phoneSuffix)
    {
        var (manager, managerToken, _) = await factory.RegisterAsync($"+88017500{phoneSuffix}1", "Manager");
        var (member, memberToken, _) = await factory.RegisterAsync($"+88017500{phoneSuffix}2", "Member Guy");
        manager.DefaultRequestHeaders.Authorization = new("Bearer", managerToken);
        member.DefaultRequestHeaders.Authorization = new("Bearer", memberToken);

        var create = await manager.PostAsJsonAsync("/api/messes", new { name = $"Mess {phoneSuffix}" });
        create.EnsureSuccessStatusCode();
        var messId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;

        var invite = await manager.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/members/invite")
        {
            Headers = { { "X-Mess-Id", messId } },
        });
        invite.EnsureSuccessStatusCode();
        var inviteToken = (await invite.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("inviteToken").GetString()!;

        var join = await member.PostAsJsonAsync("/api/members/join", new { inviteToken });
        join.EnsureSuccessStatusCode();

        var members = await manager.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/members")
        {
            Headers = { { "X-Mess-Id", messId } },
        });
        members.EnsureSuccessStatusCode();
        var list = await members.Content.ReadFromJsonAsync<JsonElement>();
        Guid IdOf(string name) => list.GetProperty("items").EnumerateArray()
            .Single(m => m.GetProperty("name").GetString() == name)
            .GetProperty("memberId").GetGuid();

        var memberId = IdOf("Member Guy");
        var approve = await manager.SendAsync(new HttpRequestMessage(HttpMethod.Put, $"/api/members/{memberId}/approve")
        {
            Headers = { { "X-Mess-Id", messId } },
        });
        approve.StatusCode.Should().Be(HttpStatusCode.NoContent);

        return new DomainTestMess(manager, member, Guid.Parse(messId), IdOf("Manager"), memberId);
    }
}

internal sealed record DomainTestMess(
    HttpClient Manager,
    HttpClient Member,
    Guid MessId,
    Guid ManagerId,
    Guid MemberId);
