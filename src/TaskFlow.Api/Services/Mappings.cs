using TaskFlow.Api.Contracts.Auth;
using TaskFlow.Api.Contracts.Projects;
using TaskFlow.Api.Contracts.Tasks;
using TaskFlow.Api.Domain.Entities;

namespace TaskFlow.Api.Services;

/// <summary>Pure entity-to-DTO projections. Keeps controllers and services free of mapping noise.</summary>
public static class Mappings
{
    public static UserResponse ToResponse(this User user) =>
        new(user.Id, user.Email, user.DisplayName, user.Role, user.CreatedAt);

    public static ProjectResponse ToResponse(this Project project, int taskCount) =>
        new(project.Id, project.Key, project.Name, project.Description,
            project.CreatedById, taskCount, project.CreatedAt, project.UpdatedAt);

    public static TaskResponse ToResponse(this TaskItem task) =>
        new(
            task.Id,
            task.ProjectId,
            task.Project?.Key ?? string.Empty,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.Assignee?.ToResponse(),
            task.CreatedById,
            task.DueDate,
            task.CreatedAt,
            task.UpdatedAt);
}
