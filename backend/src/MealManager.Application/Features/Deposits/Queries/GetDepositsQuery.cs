using FluentValidation;
using MealManager.Application.Abstractions;
using MealManager.Application.Common;
using MealManager.Application.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Deposits;

/// <summary>Deposit ledger — filters (member, status, date range), sorting (date, amount).</summary>
public record GetDepositsQuery(
    int? Page,
    int? PageSize,
    Guid? MemberId,
    string? Status,
    DateOnly? From,
    DateOnly? To,
    string? SortBy,
    string? SortDir) : IListQuery, ITenantRequest, IRequest<PaginatedResult<DepositDto>>;

public sealed class GetDepositsQueryValidator : AbstractValidator<GetDepositsQuery>
{
    public static readonly string[] SortWhitelist = ["date", "amount"];

    private static readonly string[] Statuses = ["pending", "approved", "rejected"];

    public GetDepositsQueryValidator()
    {
        ListQueryRules.Apply(this, SortWhitelist);
        RuleFor(x => x.Status)
            .Must(s => s is null || Statuses.Contains(s.Trim().ToLowerInvariant()))
            .WithMessage("status must be one of: pending, approved, rejected.");
    }
}

public sealed class GetDepositsQueryHandler(IApplicationContext db, ITenantContext tenant)
    : IRequestHandler<GetDepositsQuery, PaginatedResult<DepositDto>>
{
    public async Task<PaginatedResult<DepositDto>> Handle(GetDepositsQuery request, CancellationToken cancellationToken)
    {
        var page = Pagination.NormalizePage(request.Page);
        var pageSize = Pagination.NormalizePageSize(request.PageSize);

        var query = db.Deposits.AsNoTracking().IgnoreQueryFilters()
            .Where(d => d.MessId == tenant.MessId!.Value);
        if (request.MemberId is { } memberId)
        {
            query = query.Where(d => d.MemberId == memberId);
        }

        if (request.Status is { Length: > 0 } status)
        {
            // Parse outside the expression tree — EF cannot translate Enum.Parse.
            var parsed = Enum.Parse<DepositStatus>(status.Trim(), ignoreCase: true);
            query = query.Where(d => d.Status == parsed);
        }

        if (request.From is { } from)
        {
            query = query.Where(d => d.Date >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(d => d.Date <= to);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var ascending = request.SortsAscending();
        var ordered = request.SortKey("date") switch
        {
            "amount" => ascending
                ? query.OrderBy(d => d.Amount).ThenBy(d => d.Date)
                : query.OrderByDescending(d => d.Amount).ThenByDescending(d => d.Date),
            _ => ascending
                ? query.OrderBy(d => d.Date).ThenBy(d => d.CreatedAt)
                : query.OrderByDescending(d => d.Date).ThenByDescending(d => d.CreatedAt),
        };

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Join(db.Members.AsNoTracking(),
                d => d.MemberId, u => u.Id,
                (d, u) => new DepositDto(
                    d.Id, d.MemberId, u.Name, d.Amount, d.Date, d.Status.ToString(),
                    d.ApprovedBy, d.ApprovedAt, d.Note, d.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<DepositDto>(items, page, pageSize, totalCount);
    }
}
