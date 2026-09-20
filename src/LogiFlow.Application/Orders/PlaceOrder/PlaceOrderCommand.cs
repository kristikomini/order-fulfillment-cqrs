using MediatR;

namespace LogiFlow.Application.Orders.PlaceOrder;

public sealed record PlaceOrderCommand(string CustomerName, IReadOnlyList<PlaceOrderItem> Items)
    : IRequest<OrderDto>;

public sealed record PlaceOrderItem(Guid ProductId, int Quantity);
