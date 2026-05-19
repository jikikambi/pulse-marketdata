namespace SignalPulse.MarketData.Infrastructure.Elastic;

public sealed record WorkflowEvent(Guid CorrelationId, string Stage, string EventType, string Message, DateTimeOffset Timestamp, object? Metadata = null);