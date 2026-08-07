using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskFlow.Api.Contracts.Auth;
using TaskFlow.Api.Contracts.Common;
using TaskFlow.Api.Contracts.Projects;
using TaskFlow.Api.Contracts.Tasks;
using TaskFlow.Api.Domain.Enums;

namespace TaskFlow.Api.Tests.Integration;

public class TasksApiTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Protected_Endpoint_Returns401_WithoutToken()
    {
        var response = await _client.GetAsync("/api/v1/tasks");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_Login_And_Full_Task_Lifecycle_Works()
    {
        // Register a fresh user.
        var email = $"user-{Guid.NewGuid():N}@example.com";
        var register = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "Integration User", "Password123"));
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>(Json);
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth!.AccessToken));

        Authorize(auth.AccessToken);

        // Create a project.
        var key = $"P{Random.Shared.Next(1000, 9999)}";
        var projectResp = await _client.PostAsJsonAsync("/api/v1/projects",
            new ProjectRequest(key, "Integration Project", "Created by tests"));
        Assert.Equal(HttpStatusCode.Created, projectResp.StatusCode);
        var project = await projectResp.Content.ReadFromJsonAsync<ProjectResponse>(Json);

        // Create a task in the project.
        var createResp = await _client.PostAsJsonAsync("/api/v1/tasks",
            new CreateTaskRequest(project!.Id, "Write docs", "Document the API", TaskPriority.High));
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var task = await createResp.Content.ReadFromJsonAsync<TaskResponse>(Json);
        Assert.Equal(TaskItemStatus.Todo, task!.Status);
        Assert.Equal(TaskPriority.High, task.Priority);
        Assert.Equal(key, task.ProjectKey);

        // Transition status.
        var patchResp = await _client.PatchAsJsonAsync($"/api/v1/tasks/{task.Id}/status",
            new UpdateTaskStatusRequest(TaskItemStatus.InProgress));
        Assert.Equal(HttpStatusCode.OK, patchResp.StatusCode);
        var moved = await patchResp.Content.ReadFromJsonAsync<TaskResponse>(Json);
        Assert.Equal(TaskItemStatus.InProgress, moved!.Status);

        // List with a filter and confirm it comes back.
        var list = await _client.GetFromJsonAsync<PagedResponse<TaskResponse>>(
            $"/api/v1/tasks?projectId={project.Id}&status=InProgress", Json);
        Assert.NotNull(list);
        Assert.Contains(list!.Items, t => t.Id == task.Id);
    }

    [Fact]
    public async Task Register_Rejects_WeakPassword_With400AndProblemDetails()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest($"weak-{Guid.NewGuid():N}@example.com", "Weak", "weak"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.TryGetProperty("errors", out _));
        Assert.True(doc.RootElement.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns400()
    {
        var email = $"login-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "Login User", "Password123"));

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, "WrongPassword1"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private void Authorize(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
}
