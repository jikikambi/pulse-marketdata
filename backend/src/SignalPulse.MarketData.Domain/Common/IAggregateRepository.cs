namespace SignalPulse.MarketData.Domain.Common;

public interface IAggregateRepository
{
    Task<TAggregate?> LoadAsync<TAggregate>(Guid id, CancellationToken ct) where TAggregate : AggregateRoot, new();

    Task<PersistResult> PersistAsync<TAggregate>(TAggregate aggregate, CancellationToken ct) where TAggregate : AggregateRoot;
}
