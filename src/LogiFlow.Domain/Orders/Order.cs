using LogiFlow.Domain.Common;
using LogiFlow.Domain.Orders.Events;

namespace LogiFlow.Domain.Orders;

/// <summary>
/// Order aggregate root. Owns its lines and enforces the fulfillment state
/// machine: Pending → Confirmed → Fulfilled, or Pending → Rejected. State
/// transitions are the only public mutators and each raises a domain event.
/// </summary>
public class Order : Entity
{
    private readonly List<OrderLine> _lines = new();

    public Guid Id { get; private set; }
    public string OrderNumber { get; private set; } = default!;
    public string CustomerName { get; private set; } = default!;
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();
    public decimal Total => _lines.Sum(l => l.LineTotal);

    private Order() { } // EF Core

    /// <summary>
    /// Creates a pending order from a set of (product, quantity) items and
    /// raises <see cref="OrderPlacedEvent"/>. Does not touch inventory —
    /// reservation is a separate, explicit step (see <see cref="Confirm"/>).
    /// </summary>
    public static Order Place(string customerName, IReadOnlyCollection<OrderLineDraft> items)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            throw new DomainException("Customer name is required.");
        if (items is null || items.Count == 0)
            throw new DomainException("An order must have at least one line.");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = GenerateOrderNumber(),
            CustomerName = customerName.Trim(),
            Status = OrderStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var item in items)
            order._lines.Add(new OrderLine(item.ProductId, item.Sku, item.Quantity, item.UnitPrice));

        order.Raise(new OrderPlacedEvent(order.Id, order.OrderNumber, order.CustomerName, order.Total));
        return order;
    }

    /// <summary>Marks the order confirmed (stock reserved). Raises <see cref="OrderConfirmedEvent"/>.</summary>
    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException($"Only a pending order can be confirmed (was {Status}).");

        Status = OrderStatus.Confirmed;
        Raise(new OrderConfirmedEvent(Id, OrderNumber));
    }

    /// <summary>Rejects a pending order with a reason. Raises <see cref="OrderRejectedEvent"/>.</summary>
    public void Reject(string reason)
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException($"Only a pending order can be rejected (was {Status}).");

        Status = OrderStatus.Rejected;
        RejectionReason = reason;
        Raise(new OrderRejectedEvent(Id, OrderNumber, reason));
    }

    /// <summary>Marks a confirmed order as shipped. Raises <see cref="OrderFulfilledEvent"/>.</summary>
    public void Fulfill()
    {
        if (Status != OrderStatus.Confirmed)
            throw new DomainException($"Only a confirmed order can be fulfilled (was {Status}).");

        Status = OrderStatus.Fulfilled;
        Raise(new OrderFulfilledEvent(Id, OrderNumber));
    }

    private static string GenerateOrderNumber() =>
        $"LF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}

/// <summary>Input record for constructing an order line at placement time.</summary>
public sealed record OrderLineDraft(Guid ProductId, string Sku, int Quantity, decimal UnitPrice);
