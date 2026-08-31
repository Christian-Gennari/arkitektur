namespace arkitektur.Infrastructure.Events;

public sealed record EventTrace(
    long Sequence,
    Guid EventId,
    string CorrelationId,
    string EventType,
    string Stage,
    string? Consumer,
    string? Detail,
    DateTimeOffset RecordedAt
);

public static class EventStages
{
    public const string Queued = "queued";
    public const string Processing = "processing";
    public const string ConsumerStarted = "consumer-started";
    public const string ConsumerCompleted = "consumer-completed";
    public const string ConsumerFailed = "consumer-failed";
    public const string Completed = "completed";
    public const string CompletedWithErrors = "completed-with-errors";
}
