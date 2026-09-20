using LogiFlow.Application.Common.Interfaces;
using LogiFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogiFlow.IntegrationTests;

/// <summary>No-op metrics so handler tests don't need the telemetry stack.</summary>
public sealed class NoOpMetrics : ILogiFlowMetrics
{
    public void OrderPlaced() { }
    public void OrderConfirmed() { }
    public void OrderRejected() { }
    public void OrderFulfilled() { }
    public void OutboxMessagePublished(string eventType) { }
}

public static class TestDb
{
    public static ApplicationDbContext CreateInMemory()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"logiflow-{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }
}
