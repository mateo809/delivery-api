using System.ComponentModel.DataAnnotations;

namespace DeliveryApi.Contracts.Orders;

public sealed record CreateOrderRequest(
    [Required, MaxLength(300)] string DeliveryAddress,
    [MaxLength(500)] string? Note,
    [Required, MinLength(1), MaxLength(50)] IReadOnlyList<OrderItemRequest> Items);

public sealed record OrderItemRequest(
    [Required, MaxLength(100)] string Name,
    [Range(1, 100)] int Quantity,
    [Range(typeof(decimal), "0.01", "10000")] decimal UnitPrice);
