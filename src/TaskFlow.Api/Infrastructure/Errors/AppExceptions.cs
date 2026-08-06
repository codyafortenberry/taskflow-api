namespace TaskFlow.Api.Infrastructure.Errors;

/// <summary>Base type for expected, HTTP-mappable domain errors.</summary>
public abstract class AppException(string message) : Exception(message)
{
    public abstract int StatusCode { get; }
    public abstract string Title { get; }
}

/// <summary>404 — the requested resource does not exist.</summary>
public sealed class NotFoundException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status404NotFound;
    public override string Title => "Resource not found";
}

/// <summary>409 — the request conflicts with the current state (e.g. duplicate key).</summary>
public sealed class ConflictException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status409Conflict;
    public override string Title => "Conflict";
}

/// <summary>403 — the caller is authenticated but not permitted to perform the action.</summary>
public sealed class ForbiddenException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status403Forbidden;
    public override string Title => "Forbidden";
}

/// <summary>400 — the request is well-formed but semantically invalid.</summary>
public sealed class ValidationFailedException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
    public override string Title => "Invalid request";
}
