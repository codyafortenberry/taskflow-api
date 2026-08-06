using TaskFlow.Api.Domain.Enums;

namespace TaskFlow.Api.Domain.Entities;

/// <summary>
/// An authenticated account that can own, create and be assigned work items.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    public required string Email { get; set; }

    public required string DisplayName { get; set; }

    /// <summary>BCrypt hash of the password. The plain-text password is never stored.</summary>
    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; } = UserRole.Member;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
    public ICollection<TaskItem> CreatedTasks { get; set; } = new List<TaskItem>();
    public ICollection<Project> CreatedProjects { get; set; } = new List<Project>();
}
