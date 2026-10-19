using DeliveryApi.Domain;

namespace DeliveryApi.Contracts.Auth;

public sealed record UserResponse(Guid Id, string Email, string FullName, UserRole Role)
{
    public static UserResponse From(User user) => new(user.Id, user.Email, user.FullName, user.Role);
}
