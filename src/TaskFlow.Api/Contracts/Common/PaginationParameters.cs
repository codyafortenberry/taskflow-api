namespace TaskFlow.Api.Contracts.Common;

/// <summary>
/// Pagination options bound from the query string (<c>?page=&amp;pageSize=</c>).
/// The setters self-correct out-of-range values, so every list endpoint gets the
/// same, safe defaults from one place.
/// </summary>
public class PaginationParameters
{
    public const int MaxPageSize = 100;

    private int _page = 1;
    private int _pageSize = 20;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, MaxPageSize);
    }
}
