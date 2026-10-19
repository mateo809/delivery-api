using System.Security.Cryptography;
using DeliveryApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace DeliveryApi.Tests.Infrastructure;

public sealed class DeliveryApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string ConnectionStringVariable = "TEST_DATABASE_CONNECTION_STRING";

    private readonly string _signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    private readonly string _connectionString = new NpgsqlConnectionStringBuilder(
        Environment.GetEnvironmentVariable(ConnectionStringVariable)
        ?? throw new InvalidOperationException($"Set {ConnectionStringVariable} to run the integration tests."))
    {
        Database = $"delivery_tests_{Guid.NewGuid():N}"
    }.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Database", _connectionString);
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Jwt:SigningKey", _signingKey);
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
