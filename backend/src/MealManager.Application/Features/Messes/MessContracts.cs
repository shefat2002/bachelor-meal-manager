namespace MealManager.Application.Features.Messes;

public record MessDto(Guid Id, string Name, DateTime CreatedAt, string MyRole, int ActiveMemberCount);

public record CreateMessRequest(string Name);

public record UpdateMessRequest(string Name);
