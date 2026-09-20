using LogiFlow.Application.Common.Interfaces;
using LogiFlow.Domain.Common;
using LogiFlow.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogiFlow.Application.Orders.FulfillOrder;

public sealed class FulfillOrderHandler : IRequestHandler<FulfillOrderCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ILogiFlowMetrics _metrics;
    private readonly ILogger<FulfillOrderHandler> _logger;

    public FulfillOrderHandler(
        IApplicationDbContext db,
        ILogiFlowMetrics metrics,
        ILogger<FulfillOrderHandler> logger)
    {
        _db = db;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task Handle(FulfillOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new DomainException($"Order {request.OrderId} not found.");

        if (order.Status != OrderStatus.Confirmed)
        {
            // Idempotent: nothing to do if it already shipped or never confirmed.
            _logger.LogInformation(
                "Skipping fulfillment of {OrderNumber}; status is {Status}", order.OrderNumber, order.Status);
            return;
        }

        var productIds = order.Lines.Select(l => l.ProductId).ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var line in order.Lines)
            products[line.ProductId].ShipReserved(line.Quantity);

        order.Fulfill();
        _metrics.OrderFulfilled();
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {OrderNumber} fulfilled", order.OrderNumber);
    }
}
