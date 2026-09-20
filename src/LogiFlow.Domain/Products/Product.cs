using LogiFlow.Domain.Common;

namespace LogiFlow.Domain.Products;

/// <summary>
/// Inventory aggregate. Tracks physical stock on hand and the portion reserved
/// against confirmed orders. Available-to-promise is what a new order can draw
/// on. Reservation and shipment are the only ways stock moves.
/// </summary>
public class Product : Entity
{
    public Guid Id { get; private set; }
    public string Sku { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public decimal UnitPrice { get; private set; }
    public int StockOnHand { get; private set; }
    public int StockReserved { get; private set; }

    /// <summary>Stock a new order is allowed to reserve.</summary>
    public int AvailableToPromise => StockOnHand - StockReserved;

    private Product() { } // EF Core

    public static Product Create(string sku, string name, decimal unitPrice, int initialStock)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("SKU is required.");
        if (unitPrice < 0)
            throw new DomainException("Unit price cannot be negative.");
        if (initialStock < 0)
            throw new DomainException("Initial stock cannot be negative.");

        return new Product
        {
            Id = Guid.NewGuid(),
            Sku = sku.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            UnitPrice = unitPrice,
            StockOnHand = initialStock,
            StockReserved = 0
        };
    }

    /// <summary>
    /// Reserves <paramref name="quantity"/> units if available. Returns false
    /// (without mutating) when there is not enough available-to-promise.
    /// </summary>
    public bool TryReserve(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Reservation quantity must be positive.");
        if (AvailableToPromise < quantity)
            return false;

        StockReserved += quantity;
        return true;
    }

    /// <summary>Releases a previously held reservation (e.g. order rejected/cancelled).</summary>
    public void ReleaseReservation(int quantity)
    {
        if (quantity <= 0) return;
        StockReserved = Math.Max(0, StockReserved - quantity);
    }

    /// <summary>Ships reserved units: they leave both reserved and on-hand.</summary>
    public void ShipReserved(int quantity)
    {
        if (quantity <= 0) return;
        if (quantity > StockReserved)
            throw new DomainException($"Cannot ship {quantity}; only {StockReserved} reserved for {Sku}.");

        StockReserved -= quantity;
        StockOnHand -= quantity;
    }

    /// <summary>Replenish physical stock (goods receipt).</summary>
    public void Restock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Restock quantity must be positive.");
        StockOnHand += quantity;
    }
}
