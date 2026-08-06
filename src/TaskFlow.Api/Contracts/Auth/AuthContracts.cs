using TaskFlow.Api.Domain.Enums;

namespace TaskFlow.Api.Contracts.Auth;

/// <summary>Registration payload for creating a new account.</summary>
public record RegisterRequest(string Email, string DisplayName, string Password);

/// <summary>Login payload exchanging credentials for a bearer token.</summary>
public record LoginRequest(string Email, string Password);

/// <summary>Public projection of a user. Never exposes the password hash.</summary>
public record UserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    UserRole Role,
    DateTimeOffset CreatedAt);

/// <summary>Successful authentication result: the JWT plus the caller's profile.</summary>
public record AuthResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt,
    UserResponse User);
