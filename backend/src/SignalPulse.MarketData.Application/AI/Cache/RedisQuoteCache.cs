using Microsoft.Extensions.Caching.Distributed;
using SignalPulse.MarketData.Infrastructure.ReadModels;
using System.Text.Json;

namespace SignalPulse.MarketData.Application.AI.Cache;

public class RedisQuoteCache(IDistributedCache cache) : IQuoteCache
{
    private static string Key(string symbol) => $"quote:{symbol}:latest";

    public async Task<QuoteReadModel?> GetAsync(string symbol, CancellationToken ct = default)
    {
        var data = await cache.GetStringAsync(Key(symbol), ct);

        if (string.IsNullOrEmpty(data)) return null;

        return JsonSerializer.Deserialize<QuoteReadModel>(data);
    }

    public async Task SetAsync(string symbol, QuoteReadModel value, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value);

        await cache.SetStringAsync(Key(symbol), json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10),
            SlidingExpiration = TimeSpan.FromSeconds(10)
        }, ct);
    }
}