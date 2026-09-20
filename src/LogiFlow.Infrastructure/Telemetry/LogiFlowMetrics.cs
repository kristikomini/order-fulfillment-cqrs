using System.Diagnostics.Metrics;
using LogiFlow.Application.Common.Interfaces;

namespace LogiFlow.Infrastructure.Telemetry;

/// <summary>
/// OpenTelemetry-backed implementation of <see cref="ILogiFlowMetrics"/>.
/// Registered as a singleton; the <see cref="Meter"/> is exported by the OTLP
/// and console exporters configured in the API.
/// </summary>
public sealed class LogiFlowMetrics : ILogiFlowMetrics, IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _ordersPlaced;
    private readonly Counter<long> _ordersConfirmed;
    private readonly Counter<long> _ordersRejected;
    private readonly Counter<long> _ordersFulfilled;
    private readonly Counter<long> _outboxPublished;

    public LogiFlowMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(LogiFlowTelemetry.MeterName);
        _ordersPlaced = _meter.CreateCounter<long>("logiflow.orders.placed", unit: "{order}");
        _ordersConfirmed = _meter.CreateCounter<long>("logiflow.orders.confirmed", unit: "{order}");
        _ordersRejected = _meter.CreateCounter<long>("logiflow.orders.rejected", unit: "{order}");
        _ordersFulfilled = _meter.CreateCounter<long>("logiflow.orders.fulfilled", unit: "{order}");
        _outboxPublished = _meter.CreateCounter<long>("logiflow.outbox.published", unit: "{message}");
    }

    public void OrderPlaced() => _ordersPlaced.Add(1);
    public void OrderConfirmed() => _ordersConfirmed.Add(1);
    public void OrderRejected() => _ordersRejected.Add(1);
    public void OrderFulfilled() => _ordersFulfilled.Add(1);

    public void OutboxMessagePublished(string eventType) =>
        _outboxPublished.Add(1, new KeyValuePair<string, object?>("event.type", eventType));

    public void Dispose() => _meter.Dispose();
}
