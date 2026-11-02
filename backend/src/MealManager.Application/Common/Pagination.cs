namespace MealManager.Application.Common;

/// <summary>Envelope for every list endpoint — docs/API_CONVENTIONS.md §2.</summary>
public record PaginatedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public static class Pagination
{
    public const int DefaultPage = 1;

    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;

    public static int NormalizePage(int? page) => page is >= 1 ? page.Value : DefaultPage;

    public static int NormalizePageSize(int? pageSize) =>
        pageSize is >= 1 ? Math.Min(pageSize.Value, MaxPageSize) : DefaultPageSize;
}
