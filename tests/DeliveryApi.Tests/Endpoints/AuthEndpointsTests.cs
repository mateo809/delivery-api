using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DeliveryApi.Contracts.Auth;
using DeliveryApi.Domain;
using DeliveryApi.Tests.Infrastructure;

namespace DeliveryApi.Tests.Endpoints;

[Collection(ApiCollection.Name)]
public sealed class AuthEndpointsTests(DeliveryApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_ReturnsTokenAndUser()
    {
        var email = ApiTestClient.UniqueEmail();

        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, ApiTestClient.DefaultPassword, "Jane Doe", UserRole.Customer),
            ApiTestClient.JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = await response.ReadAsAsync<AuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.Equal(email, auth.User.Email);
        Assert.Equal(UserRole.Customer, auth.User.Role);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsConflict()
    {
        var email = ApiTestClient.UniqueEmail();
        await _client.RegisterAsync(UserRole.Customer, email);

        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email.ToUpperInvariant(), ApiTestClient.DefaultPassword, "Jane Doe", UserRole.Courier),
            ApiTestClient.JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidPayload_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("not-an-email", "short", "", UserRole.Customer),
            ApiTestClient.JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var email = ApiTestClient.UniqueEmail();
        await _client.RegisterAsync(UserRole.Courier, email);

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, ApiTestClient.DefaultPassword),
            ApiTestClient.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.ReadAsAsync<AuthResponse>();
        Assert.Equal(UserRole.Courier, auth.User.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = ApiTestClient.UniqueEmail();
        await _client.RegisterAsync(UserRole.Customer, email);

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, "wrong-password"),
            ApiTestClient.JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_ReturnsCurrentUser()
    {
        var auth = await _client.RegisterAsync(UserRole.Customer);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.ReadAsAsync<UserResponse>();
        Assert.Equal(auth.User.Id, user.Id);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
