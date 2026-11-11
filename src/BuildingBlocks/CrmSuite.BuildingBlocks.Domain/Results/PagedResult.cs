namespace CrmSuite.BuildingBlocks.Domain.Results;

/// <summary>Standard paged payload: { items, totalCount, page, pageSize }.</summary>
public sealed class PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 200);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);

        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    public IReadOnlyList<T> Items { get; }

    public int TotalCount { get; }

    public int Page { get; }

    public int PageSize { get; }

    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    public static PagedResult<T> Empty(int page = 1, int pageSize = 20) =>
        new([], 0, page, pageSize);

    public PagedResult<TOut> Map<TOut>(Func<T, TOut> mapper) =>
        new(Items.Select(mapper).ToArray(), TotalCount, Page, PageSize);
}
