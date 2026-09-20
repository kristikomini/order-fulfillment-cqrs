using System.Diagnostics;
using System.Text.Json;
using LogiFlow.Application.Common.Interfaces;
using LogiFlow.Infrastructure.Persistence;
using LogiFlow.Infrastructure.Telemetry;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LogiFlow.Infrastructure.Outbox;

/// <summary>
/// Background worker that polls the outbox and publishes unprocessed events
/// through the in-process mediator (at-least-once). Each message is marked
/// processed only after its handlers succeed; failures are recorded with an
/// attempt count and retried on the next poll.
/// </summary>
public sealed class OutboxProcessor : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 20;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor started (poll every {Seconds}s)", PollInterval.TotalSeconds);

        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox processing loop failed; will retry");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        var metrics = scope.ServiceProvider.GetRequiredService<ILogiFlowMetrics>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (messages.Count == 0)
            return;

        foreach (var message in messages)
        {
            using var activity = LogiFlowTelemetry.ActivitySource.StartActivity(
                "outbox.publish", ActivityKind.Producer);
            activity?.SetTag("outbox.message_id", message.Id);
            activity?.SetTag("outbox.event_type", message.Type);

            try
            {
                var eventType = System.Type.GetType(message.Type)
                    ?? throw new InvalidOperationException($"Cannot resolve event type '{message.Type}'.");

                var domainEvent = (INotification)JsonSerializer.Deserialize(message.Content, eventType, SerializerOptions)!;

                await publisher.Publish(domainEvent, ct);

                message.ProcessedOnUtc = DateTimeOffset.UtcNow;
                message.Error = null;
                metrics.OutboxMessagePublished(eventType.Name);
                _logger.LogInformation("Published outbox message {Type} ({Id})", eventType.Name, message.Id);
            }
            catch (Exception ex)
            {
                message.Attempts++;
                message.Error = ex.Message;
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, "Failed to publish outbox message {Id} (attempt {Attempts})",
                    message.Id, message.Attempts);
            }

            // Persist per message so a mid-batch failure does not re-publish successes.
            await db.SaveChangesAsync(ct);
        }
    }
}
