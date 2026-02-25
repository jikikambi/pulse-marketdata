namespace SignalPulse.MarketData.Infrastructure.Messaging;

// Canonical SignalR event envelope
public record SignalREvent(Guid EventId, string Type, object Payload, DateTimeOffset Timestamp, long Sequence);