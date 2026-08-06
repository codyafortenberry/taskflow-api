namespace TaskFlow.Api.Contracts.Common;

/// <summary>
/// Envelope for paginated list endpoints. Keeps list payloads self-describing
/// so clients can render pagination without extra round-trips.
/// </summary>
public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
