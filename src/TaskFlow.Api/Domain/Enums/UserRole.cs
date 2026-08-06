namespace TaskFlow.Api.Domain.Enums;

/// <summary>
/// Coarse authorization role. Members manage their own work; Admins can manage anything.
/// </summary>
public enum UserRole
{
    Member = 0,
    Admin = 1
}
