using DeliveryApi.Contracts.Common;
using DeliveryApi.Domain;

namespace DeliveryApi.Contracts.Orders;

public sealed class OrderListQuery : PageQuery
{
    public OrderStatus? Status { get; init; }
}
