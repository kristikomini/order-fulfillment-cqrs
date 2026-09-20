using LogiFlow.Application.Orders.FulfillOrder;
using LogiFlow.Domain.Orders.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LogiFlow.Application.Orders.EventHandlers;

/// <summary>
/// Reacts to <see cref="OrderConfirmedEvent"/> once it is published from the
/// outbox and triggers fulfillment. This is the "reliable side effect after
/// commit" the outbox exists to guarantee: confirmation and this reaction can
/// never diverge, because the event was persisted in the same transaction as
/// the confirmation.
/// </summary>
public sealed class OrderConfirmedEventHandler : INotificationHandler<OrderConfirmedEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<OrderConfirmedEventHandler> _logger;

    public OrderConfirmedEventHandler(ISender sender, ILogger<OrderConfirmedEventHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(OrderConfirmedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Reacting to confirmation of {OrderNumber}; dispatching fulfillment", notification.OrderNumber);

        await _sender.Send(new FulfillOrderCommand(notification.OrderId), cancellationToken);
    }
}
