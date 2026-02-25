
using Marten;
using SignalPulse.Abstractions.Events;
using SignalPulse.MarketData.Domain.Common;

namespace SignalPulse.MarketData.Infrastructure.EventStore;

public class MartenAggregateRepository(IDocumentSession session) : IAggregateRepository
{
    public async Task<TAggregate?> LoadAsync<TAggregate>(Guid id, CancellationToken ct) where TAggregate : AggregateRoot, new()
    {
        var stream = await session.Events.FetchStreamAsync(id, token: ct);
        var domainEvents = stream.Select(x => x.Data).OfType<IDomainEvent>().ToList();

        if (domainEvents.Count == 0)
            return null;

        var agg = new TAggregate();
        agg.ApplyHistory(domainEvents);
        return agg;
    }

    public async Task<PersistResult> PersistAsync<TAggregate>(TAggregate aggregate, CancellationToken ct) where TAggregate : AggregateRoot
    {
        var events = aggregate.GetUnCommittedEvents().ToList();

        if (events.Count == 0)
            return new PersistResult(true, []);

        try
        {
            // Assign EventId and Timestamp for each event (needed for client dedupe / ordering)
            var eventMetas = events.Select(e => new EventMetadata(
                EventId: Guid.NewGuid(),
                Timestamp: DateTimeOffset.UtcNow,
                Event: e
            )).ToList();

            // Persist only the original domain events
            session.Events.Append(aggregate.Id, [.. events]);
            await session.SaveChangesAsync(ct);

            // Clear uncommitted events in aggregate
            aggregate.ClearUnCommittedEvents();

            // Return metadata for publishing
            return new PersistResult(true, eventMetas);
        }
        catch
        {
            return new PersistResult(false, []);
        }
    }
}
