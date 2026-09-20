using LogiFlow.Application.Common.Interfaces;
using LogiFlow.Domain.Common;
using LogiFlow.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogiFlow.Application.Orders.PlaceOrder;

/// <summary>
/// Places an order and attempts to reserve stock for every line, all in one
/// unit of work. If any line cannot be reserved the whole order is rejected
/// and prior reservations are released — inventory is never left inconsistent.
/// The domain events raised here are drained into the transactional outbox by
/// the persistence interceptor, so they publish reliably after commit.
/// </summary>
public sealed class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand, OrderDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ILogiFlowMetrics _metrics;
    private readonly ILogger<PlaceOrderHandler> _logger;

    public PlaceOrderHandler(
        IApplicationDbContext db,
        ILogiFlowMetrics metrics,
        ILogger<PlaceOrderHandler> logger)
    {
        _db = db;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<OrderDto> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var missing = productIds.Where(id => !products.ContainsKey(id)).ToList();
        if (missing.Count != 0)
            throw new DomainException($"Unknown product(s): {string.Join(", ", missing)}.");

        var drafts = request.Items
            .Select(i =>
            {
                var p = products[i.ProductId];
                return new OrderLineDraft(p.Id, p.Sku, i.Quantity, p.UnitPrice);
            })
            .ToList();

        var order = Order.Place(request.CustomerName, drafts);
        _metrics.OrderPlaced();

        // Reserve stock line by line; roll back reservations if any line fails.
        var reservationLog = new List<(Domain.Products.Product product, int qty)>();
        var shortfall = (string?)null;

        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];
            if (product.TryReserve(item.Quantity))
            {
                reservationLog.Add((product, item.Quantity));
            }
            else
            {
                shortfall = $"Insufficient stock for {product.Sku}: requested {item.Quantity}, available {product.AvailableToPromise}.";
                break;
            }
        }

        if (shortfall is null)
        {
            order.Confirm();
            _metrics.OrderConfirmed();
            _logger.LogInformation("Order {OrderNumber} confirmed for {Customer}", order.OrderNumber, order.CustomerName);
        }
        else
        {
            foreach (var (product, qty) in reservationLog)
                product.ReleaseReservation(qty);

            order.Reject(shortfall);
            _metrics.OrderRejected();
            _logger.LogWarning("Order {OrderNumber} rejected: {Reason}", order.OrderNumber, shortfall);
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        return OrderDto.From(order);
    }
}
