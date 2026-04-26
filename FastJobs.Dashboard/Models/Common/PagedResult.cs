namespace FastJobs.Dashboard.Models.Common;

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items      { get; init; } = Array.Empty<T>();
    public int TotalCount              { get; init; }
    public int Page                    { get; init; }
    public int PageSize                { get; init; }
    public int TotalPages              => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage            => Page < TotalPages;
    public bool HasPreviousPage        => Page > 1;
}
