using MealManager.Application.Common;

namespace MealManager.Application.Features.Members;

public record MessMemberDto(Guid MemberId, string Name, string Phone, string Role, string Status, DateTime JoinedAt);

public record InviteResult(string InviteToken, DateTime ExpiresAt);

public record JoinedMessResult(Guid MessId, string MessName, string Status);

public record UpdateMemberRoleRequest(string Role);
