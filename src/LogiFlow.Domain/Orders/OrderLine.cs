using LogiFlow.Domain.Common;

namespace LogiFlow.Domain.Orders;

public class OrderLine
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = default!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => Quantity * UnitPrice;

    private OrderLine() { } // EF Core

    internal OrderLine(Guid productId, string sku, int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
            throw new DomainException("Order line quantity must be positive.");

        Id = Guid.NewGuid();
        ProductId = productId;
        Sku = sku;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
