namespace SignalPulse.MarketData.Infrastructure.RedisStore;

public interface IEventSequenceStore
{
    /// <summary>
    /// Atomically increments and returns the next sequence number.
    /// </summary>
    Task<long> GetNextAsync(CancellationToken ct = default);
}
