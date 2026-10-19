namespace DeliveryApi.Domain;

public sealed class OrderItem
{
    public OrderItem(string name, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("An item name is required.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("An item quantity must be greater than zero.");
        }

        if (unitPrice <= 0)
        {
            throw new DomainException("An item unit price must be greater than zero.");
        }

        Id = Guid.CreateVersion7();
        Name = name.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => Quantity * UnitPrice;
}
