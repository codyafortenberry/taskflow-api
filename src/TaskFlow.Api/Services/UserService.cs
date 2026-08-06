using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Contracts.Auth;
using TaskFlow.Api.Contracts.Common;
using TaskFlow.Api.Data;
using TaskFlow.Api.Infrastructure.Errors;

namespace TaskFlow.Api.Services;

public interface IUserService
{
    Task<PagedResponse<UserResponse>> ListAsync(int page, int pageSize, CancellationToken ct);
    Task<UserResponse> GetAsync(Guid id, CancellationToken ct);
}

public sealed class UserService(AppDbContext db) : IUserService
{
    public async Task<PagedResponse<UserResponse>> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        var query = db.Users.AsNoTracking().OrderBy(u => u.DisplayName);
        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => u.ToResponse())
            .ToListAsync(ct);

        return new PagedResponse<UserResponse>(items, page, pageSize, total);
    }

    public async Task<UserResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => u.ToResponse())
            .FirstOrDefaultAsync(ct);

        return user ?? throw new NotFoundException($"User '{id}' was not found.");
    }
}
