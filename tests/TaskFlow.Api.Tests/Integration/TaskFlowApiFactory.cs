using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Api.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace TaskFlow.Api.Tests.Integration;

/// <summary>
/// Boots the real API against a throwaway PostgreSQL container, so integration tests
/// exercise the genuine EF Core + Npgsql stack rather than an in-memory substitute.
/// Requires a running Docker daemon.
/// </summary>
public sealed class TaskFlowApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("taskflow_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        await _db.StartAsync();

        // Configuration is supplied via environment variables so it is present when the
        // application's top-level statements read it at CreateBuilder time — before the
        // WebApplicationFactory would otherwise inject overrides.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", _db.GetConnectionString());
        Environment.SetEnvironmentVariable("Jwt__Issuer", "taskflow-api");
        Environment.SetEnvironmentVariable("Jwt__Audience", "taskflow-clients");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "integration-test-signing-key-at-least-32-bytes-long");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenMinutes", "60");

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }
}
