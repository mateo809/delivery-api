using DeliveryApi.Auth;
using DeliveryApi.Contracts.Common;
using DeliveryApi.Contracts.Orders;
using DeliveryApi.Data;
using DeliveryApi.Domain;
using DeliveryApi.Services.Errors;
using Microsoft.EntityFrameworkCore;

namespace DeliveryApi.Services;

public sealed class OrderService(DeliveryDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<OrderResponse> CreateAsync(
        CurrentUser customer,
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var items = request.Items.Select(item => new OrderItem(item.Name, item.Quantity, item.UnitPrice));
        var order = Order.Create(customer.Id, request.DeliveryAddress, request.Note, items, timeProvider.GetUtcNow());

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        return OrderResponse.From(order);
    }

    public Task<PagedResponse<OrderSummaryResponse>> ListAsync(
        CurrentUser user,
        OrderListQuery query,
        CancellationToken cancellationToken)
    {
        var orders = user.Role == UserRole.Customer
            ? dbContext.Orders.Where(order => order.CustomerId == user.Id)
            : dbContext.Orders.Where(order => order.CourierId == user.Id);

        if (query.Status is { } status)
        {
            orders = orders.Where(order => order.Status == status);
        }

        return ToPagedSummariesAsync(orders.OrderByDescending(order => order.CreatedAt), query, cancellationToken);
    }

    public Task<PagedResponse<OrderSummaryResponse>> ListAvailableAsync(
        PageQuery query,
        CancellationToken cancellationToken)
    {
        var orders = dbContext.Orders
            .Where(order => order.Status == OrderStatus.Pending)
            .OrderBy(order => order.CreatedAt);

        return ToPagedSummariesAsync(orders, query, cancellationToken);
    }

    public async Task<OrderResponse> GetAsync(CurrentUser user, Guid orderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);

        return OrderResponse.From(EnsureVisible(user, order, orderId));
    }

    public async Task<IReadOnlyList<OrderStatusChangeResponse>> GetHistoryAsync(
        CurrentUser user,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.StatusHistory)
            .SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);

        return EnsureVisible(user, order, orderId).StatusHistory
            .OrderBy(change => change.ChangedAt)
            .Select(OrderStatusChangeResponse.From)
            .ToList();
    }

    public Task<OrderResponse> AcceptAsync(CurrentUser courier, Guid orderId, CancellationToken cancellationToken) =>
        UpdateAsync(courier, orderId, (order, now) => order.Accept(courier.Id, now), cancellationToken);

    public Task<OrderResponse> PickUpAsync(CurrentUser courier, Guid orderId, CancellationToken cancellationToken) =>
        UpdateAsync(courier, orderId, (order, now) => order.PickUp(courier.Id, now), cancellationToken);

    public Task<OrderResponse> DeliverAsync(CurrentUser courier, Guid orderId, CancellationToken cancellationToken) =>
        UpdateAsync(courier, orderId, (order, now) => order.Deliver(courier.Id, now), cancellationToken);

    public Task<OrderResponse> CancelAsync(CurrentUser customer, Guid orderId, CancellationToken cancellationToken) =>
        UpdateAsync(customer, orderId, (order, now) => order.Cancel(customer.Id, now), cancellationToken);

    private async Task<OrderResponse> UpdateAsync(
        CurrentUser user,
        Guid orderId,
        Action<Order, DateTimeOffset> change,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .Include(order => order.Items)
            .SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);

        var visibleOrder = EnsureVisible(user, order, orderId);
        change(visibleOrder, timeProvider.GetUtcNow());

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The order was modified by another request. Reload it and try again.");
        }

        return OrderResponse.From(visibleOrder);
    }

    private static Order EnsureVisible(CurrentUser user, Order? order, Guid orderId)
    {
        if (order is null || !CanView(user, order))
        {
            throw new NotFoundException($"Order {orderId} was not found.");
        }

        return order;
    }

    private static bool CanView(CurrentUser user, Order order) => user.Role switch
    {
        UserRole.Customer => order.CustomerId == user.Id,
        UserRole.Courier => order.IsAssignedTo(user.Id) || order.Status == OrderStatus.Pending,
        _ => false
    };

    private static async Task<PagedResponse<OrderSummaryResponse>> ToPagedSummariesAsync(
        IQueryable<Order> orders,
        PageQuery query,
        CancellationToken cancellationToken)
    {
        var totalCount = await orders.CountAsync(cancellationToken);

        var items = await orders
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(order => new OrderSummaryResponse(
                order.Id,
                order.Status,
                order.DeliveryAddress,
                order.TotalPrice,
                order.CourierId,
                order.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResponse<OrderSummaryResponse>(items, query.Page, query.PageSize, totalCount);
    }
}
