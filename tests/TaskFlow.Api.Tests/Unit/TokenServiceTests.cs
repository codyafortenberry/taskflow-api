using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using TaskFlow.Api.Domain.Entities;
using TaskFlow.Api.Domain.Enums;
using TaskFlow.Api.Infrastructure.Auth;
using TaskFlow.Api.Options;
using Xunit;

namespace TaskFlow.Api.Tests.Unit;

public class TokenServiceTests
{
    private static TokenService CreateService() => new(Microsoft.Extensions.Options.Options.Create(new JwtOptions
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        SigningKey = "test-signing-key-that-is-at-least-32-bytes-long",
        AccessTokenMinutes = 30
    }));

    private static User SampleUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "user@example.com",
        DisplayName = "Test User",
        PasswordHash = "x",
        Role = UserRole.Admin
    };

    [Fact]
    public void CreateAccessToken_EmbedsUserClaims()
    {
        var user = SampleUser();

        var (token, expiresAt) = CreateService().CreateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Contains(jwt.Claims, c => c.Type == "email" && c.Value == user.Email);
        // ClaimTypes.Role serializes to its full schema URI inside the raw JWT; the
        // JwtBearer handler maps it back to the role claim on validation.
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == nameof(UserRole.Admin));
        Assert.True(expiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public void CreateAccessToken_SetsConfiguredIssuerAndAudience()
    {
        var (token, _) = CreateService().CreateAccessToken(SampleUser());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("test-issuer", jwt.Issuer);
        Assert.Contains("test-audience", jwt.Audiences);
    }
}
