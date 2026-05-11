using Microsoft.SemanticKernel;
using SignalPulse.MarketData.Application.AI.Cache;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Infrastructure.Persistence;
using SignalPulse.MarketData.Infrastructure.ReadModels;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace SignalPulse.MarketData.Application.AI.Plugins;

public class QuoteInfoPlugin(IReadModelRepository<QuoteReadModel> repo,
    IQuoteCache cache) : IQuoteInfoTool
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    [KernelFunction]
    [Description("Provides historical or cached quote context for enrichment purposes.")]
    public async Task<QuoteContextResult?> GetQuoteContextAsync(string symbol)
    {
        var cached = await cache.GetAsync(symbol);

        if (cached is not null)
        {
            return FromCache(cached);
        }

        var symbolLock = _locks.GetOrAdd(symbol, _ => new SemaphoreSlim(1, 1));

        await symbolLock.WaitAsync();

        try
        {
            cached = await cache.GetAsync(symbol);

            if (cached is not null)
            {
                return FromCache(cached);
            }

            var quotes = await repo.GetAllAsync();

            if (quotes.Count == 0) return null;

            var latest = quotes.FirstOrDefault(q => q.Symbol == symbol);

            if (latest is null) return null;

            await cache.SetAsync(symbol, latest);

            return new QuoteContextResult
            {
                Price = latest.Price,
                ChangePercent = latest.ChangePercent,
                AvgPrice = quotes.Average(q => q.Price),
                Max = quotes.Max(q => q.Price),
                Min = quotes.Min(q => q.Price),
                Source = "repo"
            };
        }
        finally
        {
            symbolLock.Release();
        }
    }

    private static QuoteContextResult FromCache(QuoteReadModel q) => new()
    {
        Price = q.Price,
        ChangePercent = q.ChangePercent,
        Source = "cache"
    };
}