using TaskFlow.Api.Domain.Enums;

namespace TaskFlow.Api.Domain.Entities;

/// <summary>
/// A single unit of work: the core entity of the system.
/// </summary>
public class TaskItem
{
    public Guid Id { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    /// <summary>The user currently responsible for the task, if any.</summary>
    public Guid? AssigneeId { get; set; }
    public User? Assignee { get; set; }

    public Guid CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public DateTimeOffset? DueDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
