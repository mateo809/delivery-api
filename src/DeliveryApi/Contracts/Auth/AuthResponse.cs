namespace DeliveryApi.Contracts.Auth;

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, UserResponse User);
