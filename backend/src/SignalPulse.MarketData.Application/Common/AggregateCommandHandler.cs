using SignalPulse.Abstractions.Events;
using SignalPulse.MarketData.Domain.Common;
using SignalPulse.MarketData.Infrastructure.Messaging;
using SignalPulse.MarketData.Infrastructure.RedisStore;

namespace SignalPulse.MarketData.Application.Common;

/// <summary>
/// Base class handling persistence, idempotency, and event publishing.
/// </summary>
public abstract class AggregateCommandHandler<TAggregate>(IAggregateRepository repo,
    IIdempotencyStore idemStore,
    IDomainEventPublisher publisher)
    where TAggregate : AggregateRoot, new()
{
    protected IAggregateRepository Repository { get; } = repo;
    protected IDomainEventPublisher Publisher { get; } = publisher;

    protected async Task PersistAndPublishAsync(TAggregate aggregate, CancellationToken ct)
    {
        var result = await Repository.PersistAsync(aggregate, ct);

        if (!result.Committed) return;

        foreach (var meta in result.Events)
        {
            if (!await idemStore.TryMarkProcessedAsync(IdempotencyKeys.Event(meta.EventId), ct)) continue;

            await PublishEventAsync(meta, ct);
        }
    }

    private Task PublishEventAsync(EventMetadata meta, CancellationToken ct)
    {
        if (meta.Event is not ISignalREvent<object> evt) return Task.CompletedTask;

        return Publisher.PublishAsync(eventType: evt.EventType, eventId: meta.EventId, payload: evt.Payload, timestamp: meta.Timestamp, ct);
    }

    protected async Task<TAggregate> LoadRequiredAsync(Guid id, CancellationToken ct) => await Repository.LoadAsync<TAggregate>(id, ct)
        ?? throw new InvalidOperationException($"{typeof(TAggregate).Name} not found.");

    protected async Task<bool> CheckIdempotencyAsync(Guid requestId, CancellationToken ct) => await idemStore.TryMarkProcessedAsync(IdempotencyKeys.Command(requestId), ct);
}