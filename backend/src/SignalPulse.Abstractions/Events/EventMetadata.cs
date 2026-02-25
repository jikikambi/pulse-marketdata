namespace SignalPulse.Abstractions.Events;

public record EventMetadata( Guid EventId, DateTimeOffset Timestamp, IDomainEvent Event);