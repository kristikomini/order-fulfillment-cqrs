using FluentAssertions;
using LogiFlow.Domain.Common;
using LogiFlow.Domain.Orders;
using LogiFlow.Domain.Orders.Events;
using Xunit;

namespace LogiFlow.UnitTests.Orders;

public class OrderTests
{
    private static Order NewPendingOrder() =>
        Order.Place("ACME Logistics", new[]
        {
            new OrderLineDraft(Guid.NewGuid(), "BOX-M", 3, 1.20m),
            new OrderLineDraft(Guid.NewGuid(), "WRAP-500", 1, 6.90m)
        });

    [Fact]
    public void Place_creates_pending_order_and_raises_OrderPlaced()
    {
        var order = NewPendingOrder();

        order.Status.Should().Be(OrderStatus.Pending);
        order.Total.Should().Be(3 * 1.20m + 6.90m);
        order.OrderNumber.Should().StartWith("LF-");
        order.DomainEvents.Should().ContainSingle(e => e is OrderPlacedEvent);
    }

    [Fact]
    public void Place_with_no_lines_throws()
    {
        var act = () => Order.Place("ACME", Array.Empty<OrderLineDraft>());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Confirm_transitions_to_confirmed_and_raises_event()
    {
        var order = NewPendingOrder();
        order.ClearDomainEvents();

        order.Confirm();

        order.Status.Should().Be(OrderStatus.Confirmed);
        order.DomainEvents.Should().ContainSingle(e => e is OrderConfirmedEvent);
    }

    [Fact]
    public void Fulfill_requires_confirmed_state()
    {
        var order = NewPendingOrder(); // still pending

        var act = () => order.Fulfill();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Full_happy_path_transitions_pending_confirmed_fulfilled()
    {
        var order = NewPendingOrder();

        order.Confirm();
        order.Fulfill();

        order.Status.Should().Be(OrderStatus.Fulfilled);
        order.DomainEvents.Should().Contain(e => e is OrderFulfilledEvent);
    }

    [Fact]
    public void Reject_sets_reason_and_blocks_further_confirmation()
    {
        var order = NewPendingOrder();

        order.Reject("Insufficient stock for WRAP-500");

        order.Status.Should().Be(OrderStatus.Rejected);
        order.RejectionReason.Should().Contain("WRAP-500");
        var act = () => order.Confirm();
        act.Should().Throw<DomainException>();
    }
}
