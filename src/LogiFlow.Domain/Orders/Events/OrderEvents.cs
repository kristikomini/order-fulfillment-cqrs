using LogiFlow.Domain.Common;

namespace LogiFlow.Domain.Orders.Events;

/// <summary>Raised when a customer places an order (before stock is reserved).</summary>
public sealed record OrderPlacedEvent(Guid OrderId, string OrderNumber, string CustomerName, decimal Total)
    : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>Raised when every line has been reserved against inventory.</summary>
public sealed record OrderConfirmedEvent(Guid OrderId, string OrderNumber) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>Raised when an order cannot be satisfied.</summary>
public sealed record OrderRejectedEvent(Guid OrderId, string OrderNumber, string Reason) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>Raised when reserved stock has shipped.</summary>
public sealed record OrderFulfilledEvent(Guid OrderId, string OrderNumber) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
