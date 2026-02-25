
using Microsoft.Extensions.Options;
using SignalPulse.Persistence.Redis;
using StackExchange.Redis;

namespace SignalPulse.MarketData.Infrastructure.RedisStore;

public class RedisIdempotencyStore : IIdempotencyStore, IDisposable
{
    private readonly IConnectionMultiplexer _mux;
    private readonly IDatabase _db;
    private readonly IdempotencyOptions _opts;

    public RedisIdempotencyStore(IConnectionMultiplexer mux, IOptions<IdempotencyOptions> opts)
    {
        _mux = mux ?? throw new ArgumentNullException(nameof(mux));
        _db = _mux.GetDatabase();
        _opts = opts?.Value ?? new IdempotencyOptions();        
    }    

    public async Task<bool> TryMarkProcessedAsync(string key, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));

        // Global prefix only
        var redisKey = _opts.KeyPrefix + key;

        // Atomic SET key "1" NX PX TTL -> returns true if key was set (i.e., not processed before)
        var expiry = _opts.EntryTtl;
        var result = await _db.StringSetAsync(redisKey, "1", expiry, when: When.NotExists).ConfigureAwait(false);
        return result; // true: newly marked; false: already existed
    }

    public void Dispose()
    {
        // We don't dispose the multiplexer here because DI owns it.
    }
}