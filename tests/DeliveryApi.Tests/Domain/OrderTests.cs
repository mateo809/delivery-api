using DeliveryApi.Domain;

namespace DeliveryApi.Tests.Domain;

public sealed class OrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid CourierId = Guid.NewGuid();

    [Fact]
    public void Create_ComputesTotalPriceFromItems()
    {
        var order = CreateOrder(new OrderItem("Pizza", 2, 12.50m), new OrderItem("Soda", 3, 2m));

        Assert.Equal(31m, order.TotalPrice);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Single(order.StatusHistory);
    }

    [Fact]
    public void Create_WithoutItems_Throws()
    {
        Assert.Throws<DomainException>(() => CreateOrder());
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    public void OrderItem_WithInvalidValues_Throws(int quantity, decimal unitPrice)
    {
        Assert.Throws<DomainException>(() => new OrderItem("Pizza", quantity, unitPrice));
    }

    [Fact]
    public void FullDeliveryWorkflow_RecordsEveryStatusChange()
    {
        var order = CreateOrder(new OrderItem("Burger", 1, 9.90m));

        order.Accept(CourierId, Now.AddMinutes(1));
        order.PickUp(CourierId, Now.AddMinutes(10));
        order.Deliver(CourierId, Now.AddMinutes(25));

        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.Equal(CourierId, order.CourierId);
        Assert.Equal(Now.AddMinutes(25), order.UpdatedAt);
        Assert.Equal(
            [OrderStatus.Pending, OrderStatus.Accepted, OrderStatus.PickedUp, OrderStatus.Delivered],
            order.StatusHistory.Select(change => change.Status));
    }

    [Fact]
    public void Accept_WhenAlreadyAccepted_Throws()
    {
        var order = CreateOrder(new OrderItem("Burger", 1, 9.90m));
        order.Accept(CourierId, Now);

        Assert.Throws<DomainException>(() => order.Accept(Guid.NewGuid(), Now));
    }

    [Fact]
    public void PickUp_ByAnotherCourier_Throws()
    {
        var order = CreateOrder(new OrderItem("Burger", 1, 9.90m));
        order.Accept(CourierId, Now);

        Assert.Throws<DomainException>(() => order.PickUp(Guid.NewGuid(), Now));
    }

    [Fact]
    public void Deliver_BeforePickUp_Throws()
    {
        var order = CreateOrder(new OrderItem("Burger", 1, 9.90m));
        order.Accept(CourierId, Now);

        Assert.Throws<DomainException>(() => order.Deliver(CourierId, Now));
    }

    [Fact]
    public void Cancel_WhenPending_CancelsOrder()
    {
        var order = CreateOrder(new OrderItem("Burger", 1, 9.90m));

        order.Cancel(CustomerId, Now);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_AfterAcceptance_Throws()
    {
        var order = CreateOrder(new OrderItem("Burger", 1, 9.90m));
        order.Accept(CourierId, Now);

        Assert.Throws<DomainException>(() => order.Cancel(CustomerId, Now));
    }

    [Fact]
    public void Cancel_ByAnotherCustomer_Throws()
    {
        var order = CreateOrder(new OrderItem("Burger", 1, 9.90m));

        Assert.Throws<DomainException>(() => order.Cancel(Guid.NewGuid(), Now));
    }

    private static Order CreateOrder(params OrderItem[] items) =>
        Order.Create(CustomerId, "12 rue Sainte-Catherine, Bordeaux", null, items, Now);
}
