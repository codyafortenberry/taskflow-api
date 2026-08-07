using TaskFlow.Api.Contracts.Common;
using TaskFlow.Api.Domain.Enums;

namespace TaskFlow.Api.Contracts.Tasks;

/// <summary>
/// Filtering and sorting options for the task list endpoint, bound from the query
/// string. Paging (<c>Page</c>/<c>PageSize</c>) is inherited from <see cref="PaginationParameters"/>.
/// </summary>
public class TaskQueryParameters : PaginationParameters
{
    public Guid? ProjectId { get; set; }
    public TaskItemStatus? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public Guid? AssigneeId { get; set; }

    /// <summary>Case-insensitive substring match against title and description.</summary>
    public string? Search { get; set; }

    /// <summary>Sort key: createdAt | updatedAt | priority | dueDate. Prefix with '-' for descending.</summary>
    public string Sort { get; set; } = "-createdAt";
}
