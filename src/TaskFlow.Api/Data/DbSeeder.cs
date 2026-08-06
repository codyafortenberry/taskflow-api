using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Domain.Entities;
using TaskFlow.Api.Domain.Enums;
using TaskFlow.Api.Infrastructure.Auth;

namespace TaskFlow.Api.Data;

/// <summary>
/// Applies pending migrations and seeds a small, deterministic demo dataset so the
/// API (and Swagger) are immediately explorable. Idempotent: safe to run on every start.
/// </summary>
public static class DbSeeder
{
    public static async Task MigrateAndSeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await db.Database.MigrateAsync();

        if (await db.Users.AnyAsync())
        {
            return;
        }

        var admin = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Email = "admin@taskflow.dev",
            DisplayName = "Ada Admin",
            PasswordHash = hasher.Hash("Password123"),
            Role = UserRole.Admin
        };

        var member = new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Email = "member@taskflow.dev",
            DisplayName = "Marty Member",
            PasswordHash = hasher.Hash("Password123"),
            Role = UserRole.Member
        };

        db.Users.AddRange(admin, member);

        var project = new Project
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Key = "TASK",
            Name = "TaskFlow Platform",
            Description = "Sample project seeded for demonstration.",
            CreatedById = admin.Id
        };

        db.Projects.Add(project);

        db.Tasks.AddRange(
            new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Title = "Set up CI pipeline",
                Description = "Build, test and lint on every push.",
                Status = TaskItemStatus.InProgress,
                Priority = TaskPriority.High,
                AssigneeId = member.Id,
                CreatedById = admin.Id,
                DueDate = DateTimeOffset.UtcNow.AddDays(7)
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Title = "Design task board UI",
                Description = "Columns per status with drag-and-drop.",
                Status = TaskItemStatus.Todo,
                Priority = TaskPriority.Medium,
                CreatedById = admin.Id
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Title = "Write API integration tests",
                Status = TaskItemStatus.Done,
                Priority = TaskPriority.Low,
                AssigneeId = admin.Id,
                CreatedById = member.Id
            });

        await db.SaveChangesAsync();
    }
}
