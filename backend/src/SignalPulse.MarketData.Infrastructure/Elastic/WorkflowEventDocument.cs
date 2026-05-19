namespace SignalPulse.MarketData.Infrastructure.Elastic;

public sealed record WorkflowEventDocument
{
    public Guid CorrelationId { get; init; }
    public string Stage { get; init; } = default!;
    public string EventType { get; init; } = default!;
    public string Message { get; init; } = default!;
    public DateTimeOffset Timestamp { get; init; }

    public object? Metadata { get; init; }
}