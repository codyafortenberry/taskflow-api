using TaskFlow.Api.Domain.Enums;

namespace TaskFlow.Api.Contracts.Tasks;

/// <summary>
/// Filtering, sorting and pagination options for the task list endpoint,
/// bound from the query string.
/// </summary>
public class TaskQueryParameters
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;
    private int _page = 1;

    public Guid? ProjectId { get; set; }
    public TaskItemStatus? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public Guid? AssigneeId { get; set; }

    /// <summary>Case-insensitive substring match against title and description.</summary>
    public string? Search { get; set; }

    /// <summary>Sort key: createdAt | updatedAt | priority | dueDate. Prefix with '-' for descending.</summary>
    public string Sort { get; set; } = "-createdAt";

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => 1,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }
}
