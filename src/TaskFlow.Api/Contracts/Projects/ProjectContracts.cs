namespace TaskFlow.Api.Contracts.Projects;

/// <summary>Create or update payload for a project.</summary>
public record ProjectRequest(string Key, string Name, string? Description);

/// <summary>Full representation of a project returned to clients.</summary>
public record ProjectResponse(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    Guid CreatedById,
    int TaskCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
