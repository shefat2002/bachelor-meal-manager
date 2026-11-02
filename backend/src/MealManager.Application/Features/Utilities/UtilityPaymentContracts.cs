namespace MealManager.Application.Features.Utilities;

public record UtilityPaymentDto(
    Guid Id,
    Guid UtilityTypeId,
    string UtilityTypeName,
    decimal Amount,
    DateOnly PaymentDate,
    string? Note,
    Guid CreatedBy,
    DateTime CreatedAt);

public record CreateUtilityPaymentRequest(Guid UtilityTypeId, decimal Amount, DateOnly PaymentDate, string? Note);
