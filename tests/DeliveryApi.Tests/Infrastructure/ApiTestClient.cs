using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeliveryApi.Contracts.Auth;
using DeliveryApi.Contracts.Orders;
using DeliveryApi.Domain;

namespace DeliveryApi.Tests.Infrastructure;

public static class ApiTestClient
{
    public const string DefaultPassword = "password-used-in-tests";

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<HttpClient> CreateAuthenticatedClientAsync(this DeliveryApiFactory factory, UserRole role)
    {
        var client = factory.CreateClient();
        var auth = await client.RegisterAsync(role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    public static async Task<AuthResponse> RegisterAsync(this HttpClient client, UserRole role, string? email = null)
    {
        var request = new RegisterRequest(email ?? UniqueEmail(), DefaultPassword, "Test User", role);
        var response = await client.PostAsJsonAsync("/api/auth/register", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsAsync<AuthResponse>();
    }

    public static async Task<OrderResponse> CreateOrderAsync(this HttpClient client)
    {
        var request = new CreateOrderRequest(
            "8 place de la Bourse, Bordeaux",
            "Ring twice",
            [new OrderItemRequest("Margherita pizza", 2, 11.50m), new OrderItemRequest("Lemonade", 1, 3.20m)]);

        var response = await client.PostAsJsonAsync("/api/orders", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsAsync<OrderResponse>();
    }

    public static async Task<T> ReadAsAsync<T>(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>(JsonOptions)
        ?? throw new InvalidOperationException($"The response body could not be read as {typeof(T).Name}.");

    public static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";
}
