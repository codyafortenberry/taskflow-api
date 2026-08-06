using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Contracts.Common;
using TaskFlow.Api.Contracts.Tasks;
using TaskFlow.Api.Data;
using TaskFlow.Api.Domain.Entities;
using TaskFlow.Api.Domain.Enums;
using TaskFlow.Api.Infrastructure.Auth;
using TaskFlow.Api.Infrastructure.Errors;

namespace TaskFlow.Api.Services;

public interface ITaskService
{
    Task<PagedResponse<TaskResponse>> ListAsync(TaskQueryParameters query, CancellationToken ct);
    Task<TaskResponse> GetAsync(Guid id, CancellationToken ct);
    Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct);
    Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct);
    Task<TaskResponse> UpdateStatusAsync(Guid id, TaskItemStatus status, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

public sealed class TaskService(AppDbContext db, ICurrentUser currentUser) : ITaskService
{
    public async Task<PagedResponse<TaskResponse>> ListAsync(TaskQueryParameters query, CancellationToken ct)
    {
        var tasks = db.Tasks.AsNoTracking()
            .Include(t => t.Project)
            .Include(t => t.Assignee)
            .AsQueryable();

        if (query.ProjectId is { } projectId) tasks = tasks.Where(t => t.ProjectId == projectId);
        if (query.Status is { } status) tasks = tasks.Where(t => t.Status == status);
        if (query.Priority is { } priority) tasks = tasks.Where(t => t.Priority == priority);
        if (query.AssigneeId is { } assigneeId) tasks = tasks.Where(t => t.AssigneeId == assigneeId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            tasks = tasks.Where(t =>
                EF.Functions.ILike(t.Title, term) ||
                (t.Description != null && EF.Functions.ILike(t.Description, term)));
        }

        tasks = ApplySort(tasks, query.Sort);

        var total = await tasks.CountAsync(ct);
        var items = await tasks
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => t.ToResponse())
            .ToListAsync(ct);

        return new PagedResponse<TaskResponse>(items, query.Page, query.PageSize, total);
    }

    public async Task<TaskResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var task = await LoadWithRelations().FirstOrDefaultAsync(t => t.Id == id, ct)
                   ?? throw new NotFoundException($"Task '{id}' was not found.");
        return task.ToResponse();
    }

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct)
    {
        await EnsureProjectExists(request.ProjectId, ct);
        await EnsureAssigneeExists(request.AssigneeId, ct);

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Status = TaskItemStatus.Todo,
            Priority = request.Priority,
            AssigneeId = request.AssigneeId,
            DueDate = request.DueDate,
            CreatedById = currentUser.Id
        };

        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);

        return await GetAsync(task.Id, ct);
    }

    public async Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct)
                   ?? throw new NotFoundException($"Task '{id}' was not found.");

        await EnsureAssigneeExists(request.AssigneeId, ct);

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.AssigneeId = request.AssigneeId;
        task.DueDate = request.DueDate;

        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<TaskResponse> UpdateStatusAsync(Guid id, TaskItemStatus status, CancellationToken ct)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct)
                   ?? throw new NotFoundException($"Task '{id}' was not found.");

        task.Status = status;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct)
                   ?? throw new NotFoundException($"Task '{id}' was not found.");

        // Creators and admins may delete; anyone may otherwise edit collaboratively.
        if (!currentUser.IsAdmin && task.CreatedById != currentUser.Id)
        {
            throw new ForbiddenException("Only the task creator or an admin can delete this task.");
        }

        db.Tasks.Remove(task);
        await db.SaveChangesAsync(ct);
    }

    private IQueryable<TaskItem> LoadWithRelations() =>
        db.Tasks.AsNoTracking().Include(t => t.Project).Include(t => t.Assignee);

    private async Task EnsureProjectExists(Guid projectId, CancellationToken ct)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == projectId, ct))
        {
            throw new ValidationFailedException($"Project '{projectId}' does not exist.");
        }
    }

    private async Task EnsureAssigneeExists(Guid? assigneeId, CancellationToken ct)
    {
        if (assigneeId is { } id && !await db.Users.AnyAsync(u => u.Id == id, ct))
        {
            throw new ValidationFailedException($"Assignee '{id}' does not exist.");
        }
    }

    private static IQueryable<TaskItem> ApplySort(IQueryable<TaskItem> query, string sort)
    {
        var descending = sort.StartsWith('-');
        var key = sort.TrimStart('-').ToLowerInvariant();

        return (key, descending) switch
        {
            ("priority", true) => query.OrderByDescending(t => t.Priority),
            ("priority", false) => query.OrderBy(t => t.Priority),
            ("duedate", true) => query.OrderByDescending(t => t.DueDate),
            ("duedate", false) => query.OrderBy(t => t.DueDate),
            ("updatedat", true) => query.OrderByDescending(t => t.UpdatedAt),
            ("updatedat", false) => query.OrderBy(t => t.UpdatedAt),
            ("createdat", false) => query.OrderBy(t => t.CreatedAt),
            _ => query.OrderByDescending(t => t.CreatedAt)
        };
    }
}
