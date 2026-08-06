using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Api.Options;

/// <summary>Strongly-typed JWT configuration, validated at startup.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = default!;

    [Required]
    public string Audience { get; init; } = default!;

    /// <summary>Symmetric signing key. Must be at least 32 bytes for HS256.</summary>
    [Required, MinLength(32)]
    public string SigningKey { get; init; } = default!;

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; init; } = 60;
}
