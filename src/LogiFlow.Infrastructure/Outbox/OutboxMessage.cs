namespace LogiFlow.Infrastructure.Outbox;

/// <summary>
/// A domain event persisted in the same transaction as the state change that
/// produced it (the transactional outbox pattern). The <see cref="OutboxProcessor"/>
/// publishes unprocessed rows after commit, giving at-least-once delivery
/// without the dual-write problem between the database and a message bus.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; }

    /// <summary>Assembly-qualified CLR type name of the serialized event.</summary>
    public string Type { get; set; } = default!;

    /// <summary>JSON payload of the event.</summary>
    public string Content { get; set; } = default!;

    public DateTimeOffset OccurredOnUtc { get; set; }

    /// <summary>Null until successfully published.</summary>
    public DateTimeOffset? ProcessedOnUtc { get; set; }

    public int Attempts { get; set; }

    /// <summary>Last error, if publishing failed.</summary>
    public string? Error { get; set; }
}
