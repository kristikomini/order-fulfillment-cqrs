using FluentAssertions;
using LogiFlow.Application.Orders.PlaceOrder;
using LogiFlow.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogiFlow.IntegrationTests;

public class PlaceOrderHandlerTests
{
    [Fact]
    public async Task Placing_an_order_with_enough_stock_confirms_and_reserves()
    {
        await using var db = TestDb.CreateInMemory();
        var box = Product.Create("BOX-M", "Box", 1.20m, 10);
        db.Products.Add(box);
        await db.SaveChangesAsync();

        var handler = new PlaceOrderHandler(db, new NoOpMetrics(), NullLogger<PlaceOrderHandler>.Instance);

        var result = await handler.Handle(
            new PlaceOrderCommand("ACME", new[] { new PlaceOrderItem(box.Id, 4) }), default);

        result.Status.Should().Be("Confirmed");
        var reloaded = await db.Products.SingleAsync(p => p.Id == box.Id);
        reloaded.StockReserved.Should().Be(4);
        reloaded.AvailableToPromise.Should().Be(6);
    }

    [Fact]
    public async Task Placing_an_order_beyond_stock_rejects_and_reserves_nothing()
    {
        await using var db = TestDb.CreateInMemory();
        var box = Product.Create("BOX-M", "Box", 1.20m, 3);
        db.Products.Add(box);
        await db.SaveChangesAsync();

        var handler = new PlaceOrderHandler(db, new NoOpMetrics(), NullLogger<PlaceOrderHandler>.Instance);

        var result = await handler.Handle(
            new PlaceOrderCommand("ACME", new[] { new PlaceOrderItem(box.Id, 5) }), default);

        result.Status.Should().Be("Rejected");
        result.RejectionReason.Should().Contain("BOX-M");
        var reloaded = await db.Products.SingleAsync(p => p.Id == box.Id);
        reloaded.StockReserved.Should().Be(0);
    }

    [Fact]
    public async Task Multi_line_order_rolls_back_all_reservations_if_one_line_fails()
    {
        await using var db = TestDb.CreateInMemory();
        var plenty = Product.Create("BOX-M", "Box", 1.20m, 100);
        var scarce = Product.Create("LABEL-A6", "Label", 12m, 1);
        db.Products.AddRange(plenty, scarce);
        await db.SaveChangesAsync();

        var handler = new PlaceOrderHandler(db, new NoOpMetrics(), NullLogger<PlaceOrderHandler>.Instance);

        var result = await handler.Handle(new PlaceOrderCommand("ACME", new[]
        {
            new PlaceOrderItem(plenty.Id, 5),
            new PlaceOrderItem(scarce.Id, 10) // impossible
        }), default);

        result.Status.Should().Be("Rejected");
        (await db.Products.SingleAsync(p => p.Id == plenty.Id)).StockReserved.Should().Be(0);
        (await db.Products.SingleAsync(p => p.Id == scarce.Id)).StockReserved.Should().Be(0);
    }

    [Fact]
    public async Task Placing_an_order_writes_events_to_the_outbox_in_the_same_save()
    {
        await using var db = TestDb.CreateInMemory();
        var box = Product.Create("BOX-M", "Box", 1.20m, 10);
        db.Products.Add(box);
        await db.SaveChangesAsync();

        // The interceptor is provider-agnostic; exercise it directly here.
        var interceptor = new LogiFlow.Infrastructure.Outbox.ConvertDomainEventsToOutboxInterceptor();
        var options = new DbContextOptionsBuilder<LogiFlow.Infrastructure.Persistence.ApplicationDbContext>()
            .UseInMemoryDatabase($"logiflow-outbox-{Guid.NewGuid()}")
            .AddInterceptors(interceptor)
            .Options;
        await using var ctx = new LogiFlow.Infrastructure.Persistence.ApplicationDbContext(options);
        var p = Product.Create("BOX-M", "Box", 1.20m, 10);
        ctx.Products.Add(p);
        await ctx.SaveChangesAsync();

        var handler = new PlaceOrderHandler(ctx, new NoOpMetrics(), NullLogger<PlaceOrderHandler>.Instance);
        await handler.Handle(new PlaceOrderCommand("ACME", new[] { new PlaceOrderItem(p.Id, 2) }), default);

        // OrderPlaced + OrderConfirmed should be sitting in the outbox, unprocessed.
        var outbox = await ctx.OutboxMessages.ToListAsync();
        outbox.Should().HaveCountGreaterThanOrEqualTo(2);
        outbox.Should().OnlyContain(m => m.ProcessedOnUtc == null);
        outbox.Select(m => m.Type).Should().Contain(t => t.Contains("OrderConfirmedEvent"));
    }
}
