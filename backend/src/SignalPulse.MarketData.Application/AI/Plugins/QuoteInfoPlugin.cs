using Microsoft.SemanticKernel;
using SignalPulse.MarketData.Application.AI.Cache;
using SignalPulse.MarketData.Infrastructure.Persistence;
using SignalPulse.MarketData.Infrastructure.ReadModels;
using System.ComponentModel;

namespace SignalPulse.MarketData.Application.AI.Plugins;

public class QuoteInfoPlugin(IReadModelRepository<QuoteReadModel> repo,
    IQuoteCache cache)
{
    private static readonly SemaphoreSlim _lock = new(1, 1);

    [KernelFunction]
    [Description("Provides historical or cached quote context for enrichment purposes.")]
    public async Task<object?> GetQuoteContextAsync(string symbol)
    {
        var cached = await cache.GetAsync(symbol);

        if (cached is not null)
        {
            return new
            {
                cached.Price,
                cached.ChangePercent,
                Source = "cache"
            };
        }

        await _lock.WaitAsync();

        try
        {
            cached = await cache.GetAsync(symbol);

            if (cached is not null)
            {
                return new
                {
                    cached.Price,
                    cached.ChangePercent,
                    Source = "cache"
                };
            }

            var quotes = await repo.GetAllAsync();

            if (quotes.Count == 0) return null;

            var latest = quotes.FirstOrDefault(q => q.Symbol == symbol);

            if (latest is null) return null;

            await cache.SetAsync(symbol, latest);

            return new
            {
                latest.Price,
                latest.ChangePercent,
                avgPrice = quotes.Average(q => q.Price),
                max = quotes.Max(q => q.Price),
                min = quotes.Min(q => q.Price),
                Source = "repo"
            };
        }
        finally
        {
            _lock.Release();
        }
    }
}