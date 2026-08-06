using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Contracts.Common;
using TaskFlow.Api.Contracts.Projects;
using TaskFlow.Api.Data;
using TaskFlow.Api.Domain.Entities;
using TaskFlow.Api.Infrastructure.Auth;
using TaskFlow.Api.Infrastructure.Errors;

namespace TaskFlow.Api.Services;

public interface IProjectService
{
    Task<PagedResponse<ProjectResponse>> ListAsync(int page, int pageSize, CancellationToken ct);
    Task<ProjectResponse> GetAsync(Guid id, CancellationToken ct);
    Task<ProjectResponse> CreateAsync(ProjectRequest request, CancellationToken ct);
    Task<ProjectResponse> UpdateAsync(Guid id, ProjectRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

public sealed class ProjectService(AppDbContext db, ICurrentUser currentUser) : IProjectService
{
    public async Task<PagedResponse<ProjectResponse>> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        var query = db.Projects.AsNoTracking().OrderBy(p => p.Key);
        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => p.ToResponse(p.Tasks.Count))
            .ToListAsync(ct);

        return new PagedResponse<ProjectResponse>(items, page, pageSize, total);
    }

    public async Task<ProjectResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var project = await db.Projects.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => p.ToResponse(p.Tasks.Count))
            .FirstOrDefaultAsync(ct);

        return project ?? throw new NotFoundException($"Project '{id}' was not found.");
    }

    public async Task<ProjectResponse> CreateAsync(ProjectRequest request, CancellationToken ct)
    {
        var key = request.Key.Trim().ToUpperInvariant();

        if (await db.Projects.AnyAsync(p => p.Key == key, ct))
        {
            throw new ConflictException($"A project with key '{key}' already exists.");
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CreatedById = currentUser.Id
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);

        return project.ToResponse(0);
    }

    public async Task<ProjectResponse> UpdateAsync(Guid id, ProjectRequest request, CancellationToken ct)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct)
                      ?? throw new NotFoundException($"Project '{id}' was not found.");

        EnsureCanManage(project);

        var key = request.Key.Trim().ToUpperInvariant();
        if (key != project.Key && await db.Projects.AnyAsync(p => p.Key == key, ct))
        {
            throw new ConflictException($"A project with key '{key}' already exists.");
        }

        project.Key = key;
        project.Name = request.Name.Trim();
        project.Description = request.Description?.Trim();

        await db.SaveChangesAsync(ct);

        var taskCount = await db.Tasks.CountAsync(t => t.ProjectId == id, ct);
        return project.ToResponse(taskCount);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct)
                      ?? throw new NotFoundException($"Project '{id}' was not found.");

        EnsureCanManage(project);

        db.Projects.Remove(project);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Only the project creator or an admin may mutate/delete a project.</summary>
    private void EnsureCanManage(Project project)
    {
        if (!currentUser.IsAdmin && project.CreatedById != currentUser.Id)
        {
            throw new ForbiddenException("You do not have permission to modify this project.");
        }
    }
}
