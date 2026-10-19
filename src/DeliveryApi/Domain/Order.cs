namespace DeliveryApi.Domain;

public sealed class Order
{
    private readonly List<OrderItem> _items = [];
    private readonly List<OrderStatusChange> _statusHistory = [];

    private Order(Guid customerId, string deliveryAddress, string? note, DateTimeOffset createdAt)
    {
        Id = Guid.CreateVersion7();
        CustomerId = customerId;
        DeliveryAddress = deliveryAddress;
        Note = note;
        Status = OrderStatus.Pending;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid? CourierId { get; private set; }

    public string DeliveryAddress { get; private set; }

    public string? Note { get; private set; }

    public OrderStatus Status { get; private set; }

    public decimal TotalPrice { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public uint Version { get; private set; }

    public IReadOnlyList<OrderItem> Items => _items;

    public IReadOnlyList<OrderStatusChange> StatusHistory => _statusHistory;

    public static Order Create(
        Guid customerId,
        string deliveryAddress,
        string? note,
        IEnumerable<OrderItem> items,
        DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(deliveryAddress))
        {
            throw new DomainException("A delivery address is required.");
        }

        var order = new Order(
            customerId,
            deliveryAddress.Trim(),
            string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            createdAt);

        order._items.AddRange(items);

        if (order._items.Count == 0)
        {
            throw new DomainException("An order must contain at least one item.");
        }

        order.TotalPrice = order._items.Sum(item => item.LineTotal);
        order._statusHistory.Add(new OrderStatusChange(OrderStatus.Pending, customerId, createdAt));

        return order;
    }

    public bool IsAssignedTo(Guid courierId) => CourierId == courierId;

    public void Accept(Guid courierId, DateTimeOffset acceptedAt)
    {
        TransitionTo(OrderStatus.Accepted, courierId, acceptedAt);
        CourierId = courierId;
    }

    public void PickUp(Guid courierId, DateTimeOffset pickedUpAt)
    {
        EnsureAssignedTo(courierId);
        TransitionTo(OrderStatus.PickedUp, courierId, pickedUpAt);
    }

    public void Deliver(Guid courierId, DateTimeOffset deliveredAt)
    {
        EnsureAssignedTo(courierId);
        TransitionTo(OrderStatus.Delivered, courierId, deliveredAt);
    }

    public void Cancel(Guid customerId, DateTimeOffset cancelledAt)
    {
        if (customerId != CustomerId)
        {
            throw new DomainException("Only the customer who placed the order can cancel it.");
        }

        TransitionTo(OrderStatus.Cancelled, customerId, cancelledAt);
    }

    private void EnsureAssignedTo(Guid courierId)
    {
        if (!IsAssignedTo(courierId))
        {
            throw new DomainException("Only the assigned courier can update this order.");
        }
    }

    private void TransitionTo(OrderStatus nextStatus, Guid changedById, DateTimeOffset changedAt)
    {
        if (!IsAllowedTransition(Status, nextStatus))
        {
            throw new DomainException($"An order cannot move from {Status} to {nextStatus}.");
        }

        Status = nextStatus;
        UpdatedAt = changedAt;
        _statusHistory.Add(new OrderStatusChange(nextStatus, changedById, changedAt));
    }

    private static bool IsAllowedTransition(OrderStatus currentStatus, OrderStatus nextStatus) =>
        (currentStatus, nextStatus) switch
        {
            (OrderStatus.Pending, OrderStatus.Accepted) => true,
            (OrderStatus.Pending, OrderStatus.Cancelled) => true,
            (OrderStatus.Accepted, OrderStatus.PickedUp) => true,
            (OrderStatus.PickedUp, OrderStatus.Delivered) => true,
            _ => false
        };
}
