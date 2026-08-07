using TaskFlow.Api.Infrastructure.Auth;

namespace TaskFlow.Api.Tests.Unit;

public class PasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ProducesVerifiableHash()
    {
        var hash = _hasher.Hash("Password123");

        Assert.NotEqual("Password123", hash);
        Assert.True(_hasher.Verify("Password123", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongPassword()
    {
        var hash = _hasher.Hash("Password123");
        Assert.False(_hasher.Verify("WrongPassword", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForMalformedHash()
    {
        Assert.False(_hasher.Verify("anything", "not-a-real-bcrypt-hash"));
    }

    [Fact]
    public void Hash_IsSalted_SoIdenticalPasswordsDiffer()
    {
        Assert.NotEqual(_hasher.Hash("Password123"), _hasher.Hash("Password123"));
    }
}
