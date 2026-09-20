using System.Text.Json.Serialization;
using LogiFlow.Api.Middleware;
using LogiFlow.Application;
using LogiFlow.Infrastructure;
using LogiFlow.Infrastructure.Persistence;
using LogiFlow.Infrastructure.Telemetry;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// ---- Clean Architecture layers ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---- API surface ----
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---- Health checks ----
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database", tags: new[] { "ready" });

// ---- OpenTelemetry: traces, metrics, logs ----
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
var useOtlp = !string.IsNullOrWhiteSpace(otlpEndpoint);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(LogiFlowTelemetry.ServiceName, serviceVersion: "1.0.0"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource(LogiFlowTelemetry.ActivitySourceName);
        if (useOtlp) tracing.AddOtlpExporter();
        else if (builder.Environment.IsDevelopment()) tracing.AddConsoleExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter(LogiFlowTelemetry.MeterName);
        if (useOtlp) metrics.AddOtlpExporter();
        else if (builder.Environment.IsDevelopment()) metrics.AddConsoleExporter();
    });

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    if (useOtlp) logging.AddOtlpExporter();
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// Liveness: process is up. Readiness: dependencies (DB) reachable.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapHealthChecks("/health");

// Apply migrations + seed on startup.
await DatabaseInitializer.InitializeAsync(app.Services);

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program { }
