using DeliveryApi.Domain;

namespace DeliveryApi.Contracts.Orders;

public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    Guid? CourierId,
    OrderStatus Status,
    string DeliveryAddress,
    string? Note,
    decimal TotalPrice,
    IReadOnlyList<OrderItemResponse> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static OrderResponse From(Order order) => new(
        order.Id,
        order.CustomerId,
        order.CourierId,
        order.Status,
        order.DeliveryAddress,
        order.Note,
        order.TotalPrice,
        order.Items.Select(OrderItemResponse.From).ToList(),
        order.CreatedAt,
        order.UpdatedAt);
}

public sealed record OrderItemResponse(Guid Id, string Name, int Quantity, decimal UnitPrice, decimal LineTotal)
{
    public static OrderItemResponse From(OrderItem item) =>
        new(item.Id, item.Name, item.Quantity, item.UnitPrice, item.LineTotal);
}
