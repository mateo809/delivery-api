namespace DeliveryApi.Domain;

public sealed class OrderStatusChange
{
    public OrderStatusChange(OrderStatus status, Guid changedById, DateTimeOffset changedAt)
    {
        Id = Guid.CreateVersion7();
        Status = status;
        ChangedById = changedById;
        ChangedAt = changedAt;
    }

    public Guid Id { get; private set; }

    public OrderStatus Status { get; private set; }

    public Guid ChangedById { get; private set; }

    public DateTimeOffset ChangedAt { get; private set; }
}
