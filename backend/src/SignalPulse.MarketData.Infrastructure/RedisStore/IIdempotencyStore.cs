namespace SignalPulse.MarketData.Infrastructure.RedisStore;

public interface IIdempotencyStore
{
    /// <summary>
    /// Atomically tries to mark the given key as processed.
    /// Returns true if it was marked now (meaning caller should proceed).
    /// Returns false if it already existed (meaning caller should skip). 
    /// </summary>
    Task<bool> TryMarkProcessedAsync(string key, CancellationToken ct);
}
