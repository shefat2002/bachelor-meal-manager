namespace MealManager.Application.Features.Deposits;

public record DepositDto(
    Guid Id,
    Guid MemberId,
    string MemberName,
    decimal Amount,
    DateOnly Date,
    string Status,
    Guid? ApprovedBy,
    DateTime? ApprovedAt,
    string? Note,
    DateTime CreatedAt);

public record CreateDepositRequest(Guid? MemberId, decimal Amount, DateOnly Date, string? Note);
