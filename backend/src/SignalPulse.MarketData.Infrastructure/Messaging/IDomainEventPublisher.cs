namespace SignalPulse.MarketData.Infrastructure.Messaging;

public interface IDomainEventPublisher
{
    /// <summary>
    /// Publishes a domain event (or DTO) to external subscribers (e.g., SignalR clients).
    /// </summary>
    Task PublishAsync(string eventType, Guid eventId, object payload, DateTimeOffset timestamp, CancellationToken ct = default);
}