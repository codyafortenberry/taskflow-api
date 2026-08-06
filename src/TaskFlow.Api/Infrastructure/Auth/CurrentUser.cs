using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TaskFlow.Api.Domain.Enums;
using TaskFlow.Api.Infrastructure.Errors;

namespace TaskFlow.Api.Infrastructure.Auth;

/// <summary>Ambient accessor for the authenticated caller, derived from the JWT claims.</summary>
public interface ICurrentUser
{
    Guid Id { get; }
    bool IsAdmin { get; }
    bool IsAuthenticated { get; }
}

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public bool IsAdmin => Principal?.IsInRole(nameof(UserRole.Admin)) ?? false;

    public Guid Id
    {
        get
        {
            // JwtRegisteredClaimNames.Sub is mapped to NameIdentifier by the handler.
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);

            return Guid.TryParse(value, out var id)
                ? id
                : throw new ForbiddenException("The access token does not identify a valid user.");
        }
    }
}
