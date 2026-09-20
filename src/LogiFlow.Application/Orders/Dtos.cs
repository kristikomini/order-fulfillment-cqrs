using LogiFlow.Domain.Orders;

namespace LogiFlow.Application.Orders;

public sealed record OrderLineDto(Guid ProductId, string Sku, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    string CustomerName,
    string Status,
    DateTimeOffset CreatedAt,
    decimal Total,
    string? RejectionReason,
    IReadOnlyList<OrderLineDto> Lines)
{
    public static OrderDto From(Order order) => new(
        order.Id,
        order.OrderNumber,
        order.CustomerName,
        order.Status.ToString(),
        order.CreatedAt,
        order.Total,
        order.RejectionReason,
        order.Lines.Select(l => new OrderLineDto(l.ProductId, l.Sku, l.Quantity, l.UnitPrice, l.LineTotal)).ToList());
}
