using StackExchange.Redis;

namespace SignalPulse.MarketData.Infrastructure.RedisStore;

public sealed class RedisEventSequenceStore(IConnectionMultiplexer mux) : IEventSequenceStore
{
    private readonly IDatabase _redis = mux.GetDatabase();
    private const string Key = "event-sequence";

    public async Task<long> GetNextAsync(CancellationToken ct = default)
    {
        return await _redis.StringIncrementAsync(Key);
    }
}
