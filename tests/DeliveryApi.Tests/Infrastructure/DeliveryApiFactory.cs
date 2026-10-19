using DeliveryApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace DeliveryApi.Tests.Infrastructure;

public sealed class DeliveryApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DefaultConnectionString = "Host=localhost;Port=5432;Username=postgres;Password=postgres";

    private readonly string _connectionString = new NpgsqlConnectionStringBuilder(
        Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION_STRING") ?? DefaultConnectionString)
    {
        Database = $"delivery_tests_{Guid.NewGuid():N}"
    }.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Database", _connectionString);
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-that-is-long-enough");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync()
    {
        await using (var scope = Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<DeliveryDbContext>().Database.EnsureDeletedAsync();
        }

        await DisposeAsync();
    }
}
