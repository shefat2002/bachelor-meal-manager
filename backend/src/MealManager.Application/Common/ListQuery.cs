using FluentValidation;

namespace MealManager.Application.Common;

/// <summary>Shared shape of every Phase 2 list endpoint's paging/filter params (docs/API_CONVENTIONS.md §3).</summary>
public interface IListQuery
{
    int? Page { get; }

    int? PageSize { get; }

    DateOnly? From { get; }

    DateOnly? To { get; }

    string? SortBy { get; }

    string? SortDir { get; }
}

public static class ListQueryRules
{
    /// <summary>Applies the API_CONVENTIONS §3 rules: page ≥ 1, pageSize 1–100, from ≤ to, sortBy whitelist, sortDir asc|desc.</summary>
    public static void Apply<T>(AbstractValidator<T> validator, string[] sortWhitelist)
        where T : IListQuery
    {
        validator.RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        validator.RuleFor(x => x.PageSize).InclusiveBetween(1, Pagination.MaxPageSize)
            .WithMessage($"pageSize must be between 1 and {Pagination.MaxPageSize}.");
        validator.RuleFor(x => x.To)
            .Must((query, to) => query.From is null || to is null || query.From <= to)
            .WithMessage("'to' must be on or after 'from'.");
        validator.RuleFor(x => x.SortBy)
            .Must(sort => sort is null || sortWhitelist.Contains(sort.Trim().ToLowerInvariant()))
            .WithMessage($"sortBy must be one of: {string.Join(", ", sortWhitelist)}.");
        validator.RuleFor(x => x.SortDir)
            .Must(dir => dir is null || dir is "asc" or "desc")
            .WithMessage("sortDir must be 'asc' or 'desc'.");
    }
}

public static class ListQueryExtensions
{
    /// <summary>Validated (or defaulted) sort key, lowercased — the handler switches on these values.</summary>
    public static string SortKey(this IListQuery query, string fallback) =>
        query.SortBy is { Length: > 0 } ? query.SortBy.Trim().ToLowerInvariant() : fallback;

    public static bool SortsAscending(this IListQuery query) => query.SortDir is "asc";
}
