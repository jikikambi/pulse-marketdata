using Microsoft.Extensions.Caching.Distributed;
using SignalPulse.MarketData.Application.AI.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace SignalPulse.MarketData.Application.AI.Services.Memory;

public sealed class RedisAgentStateStore(IDistributedCache cache,
    IConnectionMultiplexer redis) : IAgentStateStore
{
    private const string IndexKey = "agent:index";
    private const string PlannerCachePrefix = "planner:cache:";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task SetAsync(string key, MarketAgentState state)
    {
        var json = JsonSerializer.Serialize(state, Options);

        await cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        var db = redis.GetDatabase();
        await db.SetAddAsync(IndexKey, key);
    }

    public async Task<MarketAgentState?> GetAsync(string key)
    {
        var json = await cache.GetStringAsync(key);

        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<MarketAgentState>(json, Options);
    }

    public async Task DeleteAsync(string key)
    {
        await cache.RemoveAsync(key);

        var db = redis.GetDatabase();
        await db.SetRemoveAsync(IndexKey, key);
        await db.KeyExpireAsync(IndexKey, TimeSpan.FromHours(2));
    }

    public async Task<IEnumerable<string>> GetKeysAsync(string pattern)
    {
        var db = redis.GetDatabase();

        var values = await db.SetMembersAsync(IndexKey);

        return [.. values.Select(v => (string)v!)];
    }

    public async Task<string?> GetPlanCacheAsync(string cacheKey) => await cache.GetStringAsync($"{PlannerCachePrefix}{cacheKey}");

    public async Task SetPlanCacheAsync(string cacheKey, string planJson)
    {
        await cache.SetStringAsync($"{PlannerCachePrefix}{cacheKey}",
            planJson,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
            });
    }
}