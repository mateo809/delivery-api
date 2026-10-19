using DeliveryApi.Domain;

namespace DeliveryApi.Contracts.Orders;

public sealed record OrderStatusChangeResponse(OrderStatus Status, Guid ChangedById, DateTimeOffset ChangedAt)
{
    public static OrderStatusChangeResponse From(OrderStatusChange change) =>
        new(change.Status, change.ChangedById, change.ChangedAt);
}
