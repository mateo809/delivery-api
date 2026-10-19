using DeliveryApi.Domain;

namespace DeliveryApi.Contracts.Orders;

public sealed record OrderSummaryResponse(
    Guid Id,
    OrderStatus Status,
    string DeliveryAddress,
    decimal TotalPrice,
    Guid? CourierId,
    DateTimeOffset CreatedAt);
