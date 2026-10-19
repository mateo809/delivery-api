using DeliveryApi.Domain;

namespace DeliveryApi.Auth;

public static class Roles
{
    public const string Customer = nameof(UserRole.Customer);

    public const string Courier = nameof(UserRole.Courier);
}
