using DeliveryApi.Domain;

namespace DeliveryApi.Auth;

public sealed record CurrentUser(Guid Id, UserRole Role);
