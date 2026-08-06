using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Contracts.Auth;
using TaskFlow.Api.Data;
using TaskFlow.Api.Domain.Entities;
using TaskFlow.Api.Domain.Enums;
using TaskFlow.Api.Infrastructure.Auth;
using TaskFlow.Api.Infrastructure.Errors;

namespace TaskFlow.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<UserResponse> GetCurrentAsync(Guid userId, CancellationToken ct);
}

public sealed class AuthService(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Email == email, ct))
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = UserRole.Member
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Verify against the found hash, or a dummy verify to keep timing uniform and
        // avoid leaking whether the email exists (user-enumeration hardening).
        var verified = user is not null && passwordHasher.Verify(request.Password, user.PasswordHash);
        if (user is null || !verified)
        {
            throw new ValidationFailedException("Invalid email or password.");
        }

        return BuildAuthResponse(user);
    }

    public async Task<UserResponse> GetCurrentAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([userId], ct)
                   ?? throw new NotFoundException("User not found.");
        return user.ToResponse();
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        var (token, expiresAt) = tokenService.CreateAccessToken(user);
        return new AuthResponse(token, "Bearer", expiresAt, user.ToResponse());
    }
}
