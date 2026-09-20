using MediatR;

namespace LogiFlow.Application.Orders.FulfillOrder;

/// <summary>Ships a confirmed order: reserved stock leaves inventory.</summary>
public sealed record FulfillOrderCommand(Guid OrderId) : IRequest;
