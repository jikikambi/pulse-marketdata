namespace SignalPulse.MarketData.Infrastructure.Messaging;

// Canonical SignalR event envelope
public record SignalREvent(string Type, Guid EventId, object Payload, DateTimeOffset Timestamp, long Sequence);