namespace TaskFlow.Api.Domain.Entities;

/// <summary>
/// A container that groups related work items, similar to a Jira project.
/// </summary>
public class Project
{
    public Guid Id { get; set; }

    /// <summary>Short uppercase key used as a human-friendly handle, e.g. "PLAT".</summary>
    public required string Key { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public Guid CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
