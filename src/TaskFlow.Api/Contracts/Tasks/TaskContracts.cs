using TaskFlow.Api.Contracts.Auth;
using TaskFlow.Api.Domain.Enums;

namespace TaskFlow.Api.Contracts.Tasks;

/// <summary>Payload for creating a task. Status defaults to Todo when omitted.</summary>
public record CreateTaskRequest(
    Guid ProjectId,
    string Title,
    string? Description,
    TaskPriority Priority = TaskPriority.Medium,
    Guid? AssigneeId = null,
    DateTimeOffset? DueDate = null);

/// <summary>Payload for a full update (PUT) of a task.</summary>
public record UpdateTaskRequest(
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    Guid? AssigneeId,
    DateTimeOffset? DueDate);

/// <summary>Payload for the lightweight status transition endpoint.</summary>
public record UpdateTaskStatusRequest(TaskItemStatus Status);

/// <summary>Full representation of a task returned to clients.</summary>
public record TaskResponse(
    Guid Id,
    Guid ProjectId,
    string ProjectKey,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    UserResponse? Assignee,
    Guid CreatedById,
    DateTimeOffset? DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
