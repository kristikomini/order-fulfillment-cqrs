using System.Diagnostics;

namespace LogiFlow.Infrastructure.Telemetry;

/// <summary>
/// Central names/sources for LogiFlow's OpenTelemetry instrumentation. The API
/// registers these with the tracer and meter providers; Infrastructure emits
/// spans and metrics through them.
/// </summary>
public static class LogiFlowTelemetry
{
    public const string ServiceName = "LogiFlow";
    public const string ActivitySourceName = "LogiFlow";
    public const string MeterName = "LogiFlow";

    /// <summary>ActivitySource for custom spans (e.g. outbox publishing).</summary>
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
