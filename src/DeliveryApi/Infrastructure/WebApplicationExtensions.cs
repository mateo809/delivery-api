using DeliveryApi.Data;
using Microsoft.EntityFrameworkCore;

namespace DeliveryApi.Infrastructure;

internal static class WebApplicationExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
