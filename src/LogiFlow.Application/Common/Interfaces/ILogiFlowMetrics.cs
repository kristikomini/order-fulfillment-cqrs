namespace LogiFlow.Application.Common.Interfaces;

/// <summary>
/// Custom business metrics surfaced through OpenTelemetry. Implemented in
/// Infrastructure over a <see cref="System.Diagnostics.Metrics.Meter"/> so the
/// Application layer stays free of the telemetry stack.
/// </summary>
public interface ILogiFlowMetrics
{
    void OrderPlaced();
    void OrderConfirmed();
    void OrderRejected();
    void OrderFulfilled();
    void OutboxMessagePublished(string eventType);
}
