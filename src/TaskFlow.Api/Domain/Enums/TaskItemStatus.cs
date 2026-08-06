namespace TaskFlow.Api.Domain.Enums;

/// <summary>
/// Lifecycle state of a work item, modelled loosely on a simple Kanban board.
/// </summary>
public enum TaskItemStatus
{
    Todo = 0,
    InProgress = 1,
    InReview = 2,
    Done = 3
}
