using System.Net;
using System.Net.Http.Json;
using DeliveryApi.Contracts.Common;
using DeliveryApi.Contracts.Orders;
using DeliveryApi.Domain;
using DeliveryApi.Tests.Infrastructure;

namespace DeliveryApi.Tests.Endpoints;

[Collection(ApiCollection.Name)]
public sealed class OrderEndpointsTests(DeliveryApiFactory factory)
{
    [Fact]
    public async Task CreateOrder_AsCustomer_ReturnsCreatedOrder()
    {
        var customer = await factory.CreateAuthenticatedClientAsync(UserRole.Customer);

        var order = await customer.CreateOrderAsync();

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(26.20m, order.TotalPrice);
        Assert.Equal(2, order.Items.Count);
        Assert.Null(order.CourierId);
    }

    [Fact]
    public async Task CreateOrder_AsCourier_ReturnsForbidden()
    {
        var courier = await factory.CreateAuthenticatedClientAsync(UserRole.Courier);

        var response = await courier.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("Bordeaux", null, [new OrderItemRequest("Pizza", 1, 10m)]),
            ApiTestClient.JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WithoutItems_ReturnsBadRequest()
    {
        var customer = await factory.CreateAuthenticatedClientAsync(UserRole.Customer);

        var response = await customer.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("Bordeaux", null, []),
            ApiTestClient.JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("Bordeaux", null, [new OrderItemRequest("Pizza", 1, 10m)]),
            ApiTestClient.JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_OfAnotherCustomer_ReturnsNotFound()
    {
        var owner = await factory.CreateAuthenticatedClientAsync(UserRole.Customer);
        var otherCustomer = await factory.CreateAuthenticatedClientAsync(UserRole.Customer);
        var order = await owner.CreateOrderAsync();

        var response = await otherCustomer.GetAsync($"/api/orders/{order.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeliveryWorkflow_UpdatesStatusAndHistory()
    {
        var customer = await factory.CreateAuthenticatedClientAsync(UserRole.Customer);
        var courier = await factory.CreateAuthenticatedClientAsync(UserRole.Courier);
        var order = await customer.CreateOrderAsync();

        var accepted = await PostAndReadAsync(courier, $"/api/orders/{order.Id}/accept");
        var pickedUp = await PostAndReadAsync(courier, $"/api/orders/{order.Id}/pickup");
        var delivered = await PostAndReadAsync(courier, $"/api/orders/{order.Id}/deliver");

        Assert.Equal(OrderStatus.Accepted, accepted.Status);
        Assert.Equal(OrderStatus.PickedUp, pickedUp.Status);
        Assert.Equal(OrderStatus.Delivered, delivered.Status);
        Assert.NotNull(delivered.CourierId);

        var history = await (await customer.GetAsync($"/api/orders/{order.Id}/history"))
            .ReadAsAsync<List<OrderStatusChangeResponse>>();

        Assert.Equal(
            [OrderStatus.Pending, OrderStatus.Accepted, OrderStatus.PickedUp, OrderStatus.Delivered],
            history.Select(change => change.Status));
    }

    [Fact]
    public async Task AcceptOrder_AlreadyAcceptedByAnotherCourier_ReturnsNotFound()
    {
        var customer = await factory.CreateAuthenticatedClientAsync(UserRole.Customer);
        var firstCourier = await factory.CreateAuthenticatedClientAsync(UserRole.Courier);
        var secondCourier = await factory.CreateAuthenticatedClientAsync(UserRole.Courier);
        var order = await customer.CreateOrderAsync();
        await PostAndReadAsync(firstCourier, $"/api/orders/{order.Id}/accept");

        var response = await secondCourier.PostAsync($"/api/orders/{order.Id}/accept", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeliverOrder_BeforePickUp_ReturnsConflict()
    {
        var customer = await factory.CreateAuthenticatedClientAsync(UserRole.Customer);
        var courier = await factory.CreateAuthenticatedClientAsync(UserRole.Courier);
        var order = await customer.CreateOrderAsync();
        await PostAndReadAsync(courier, $"/api/orders/{order.Id}/accept");

        var response = await courier.PostAsync($"/api/orders/{order.Id}/deliver", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CancelOrder_WhenPending_ReturnsCancelledOrder()
    {
        var customer = await factory.CreateAuthenticatedClientAsync(UserRole.Customer);
        var order = await customer.CreateOrderAsync();

        var cancelled = await PostAndReadAsync(customer, $"/api/orders/{order.Id}/cancel");

        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);
    }

    [Fact]
    public async Task CancelOrder_AfterAcceptance_ReturnsConflict()
    {
        var customer = await factory.CreateAuthenticatedClientAsync(UserRole.Customer);
        var courier = await factory.CreateAuthenticatedClientAsync(UserRole.Courier);
        var order = await customer.CreateOrderAsync();
        await PostAndReadAsync(courier, $"/api/orders/{order.Id}/accept");

        var response = await customer.PostAsync($"/api/orders/{order.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ListOrders_ReturnsOnlyOrdersOfCurrentCustomer()
    {
        var customer = await factory.CreateAuthenticatedClientAsync(UserRole.Customer);
        var otherCustomer = await factory.CreateAuthenticatedClientAsync(UserRole.Customer);
        await customer.CreateOrderAsync();
        await customer.CreateOrderAsync();
        await otherCustomer.CreateOrderAsync();

        var page = await (await customer.GetAsync("/api/orders?page=1&pageSize=10"))
            .ReadAsAsync<PagedResponse<OrderSummaryResponse>>();

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
    }

    [Fact]
    public async Task ListOrders_FiltersByStatus()
    {
        var customer = await factory.CreateAuthenticatedClientAsync(UserRole.Customer);
        var cancelledOrder = await customer.CreateOrderAsync();
        await customer.CreateOrderAsync();
        await PostAndReadAsync(customer, $"/api/orders/{cancelledOrder.Id}/cancel");

        var page = await (await customer.GetAsync("/api/orders?status=Cancelled"))
            .ReadAsAsync<PagedResponse<OrderSummaryResponse>>();

        var summary = Assert.Single(page.Items);
        Assert.Equal(cancelledOrder.Id, summary.Id);
    }

    [Fact]
    public async Task ListAvailableOrders_AsCourier_ContainsPendingOrder()
    {
        var customer = await factory.CreateAuthenticatedClientAsync(UserRole.Customer);
        var courier = await factory.CreateAuthenticatedClientAsync(UserRole.Courier);
        var order = await customer.CreateOrderAsync();

        var page = await (await courier.GetAsync("/api/orders/available?pageSize=100"))
            .ReadAsAsync<PagedResponse<OrderSummaryResponse>>();

        Assert.Contains(page.Items, summary => summary.Id == order.Id);
        Assert.All(page.Items, summary => Assert.Equal(OrderStatus.Pending, summary.Status));
    }

    [Fact]
    public async Task ListAvailableOrders_AsCustomer_ReturnsForbidden()
    {
        var customer = await factory.CreateAuthenticatedClientAsync(UserRole.Customer);

        var response = await customer.GetAsync("/api/orders/available");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<OrderResponse> PostAndReadAsync(HttpClient client, string url)
    {
        var response = await client.PostAsync(url, null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadAsAsync<OrderResponse>();
    }
}
