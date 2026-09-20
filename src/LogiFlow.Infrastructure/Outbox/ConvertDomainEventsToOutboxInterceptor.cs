using System.Text.Json;
using LogiFlow.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LogiFlow.Infrastructure.Outbox;

/// <summary>
/// EF Core save interceptor that converts every pending domain event on tracked
/// aggregates into <see cref="OutboxMessage"/> rows, added to the same
/// <c>SaveChanges</c> unit of work. Because the outbox insert and the state
/// change commit together, an event can never be lost after a state change nor
/// published for a change that rolled back.
/// </summary>
public sealed class ConvertDomainEventsToOutboxInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            ConvertDomainEvents(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            ConvertDomainEvents(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ConvertDomainEvents(DbContext context)
    {
        var aggregates = context.ChangeTracker
            .Entries<Entity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        if (aggregates.Count == 0)
            return;

        var messages = new List<OutboxMessage>();

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                messages.Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = domainEvent.GetType().AssemblyQualifiedName!,
                    Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions),
                    OccurredOnUtc = domainEvent.OccurredAt
                });
            }

            aggregate.ClearDomainEvents();
        }

        context.Set<OutboxMessage>().AddRange(messages);
    }
}
