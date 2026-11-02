namespace MealManager.Application.Features.Utilities;

public record UtilityTypeDto(Guid Id, string Name, string? Description, bool IsActive, DateTime CreatedAt);

public record CreateUtilityTypeRequest(string Name, string? Description);

public record UpdateUtilityTypeRequest(string Name, string? Description, bool IsActive);
