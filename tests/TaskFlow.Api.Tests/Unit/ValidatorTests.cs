using TaskFlow.Api.Contracts.Auth;
using TaskFlow.Api.Contracts.Tasks;
using TaskFlow.Api.Validation;

namespace TaskFlow.Api.Tests.Unit;

public class ValidatorTests
{
    [Theory]
    [InlineData("", "Test User", "Password123", false)]          // missing email
    [InlineData("not-an-email", "Test User", "Password123", false)]
    [InlineData("user@example.com", "Test User", "short", false)] // weak password
    [InlineData("user@example.com", "Test User", "alllowercase1", false)] // no uppercase
    [InlineData("user@example.com", "Test User", "Password123", true)]
    public void RegisterValidator_EnforcesRules(string email, string name, string password, bool expectedValid)
    {
        var result = new RegisterRequestValidator().Validate(new RegisterRequest(email, name, password));
        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact]
    public void CreateTaskValidator_RejectsEmptyTitle()
    {
        var request = new CreateTaskRequest(Guid.NewGuid(), Title: "", Description: null);
        var result = new CreateTaskRequestValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTaskRequest.Title));
    }

    [Fact]
    public void CreateTaskValidator_RejectsPastDueDate()
    {
        var request = new CreateTaskRequest(
            Guid.NewGuid(), "Valid title", null, DueDate: DateTimeOffset.UtcNow.AddDays(-1));
        var result = new CreateTaskRequestValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTaskRequest.DueDate));
    }
}
