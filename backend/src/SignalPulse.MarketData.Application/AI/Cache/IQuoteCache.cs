using SignalPulse.MarketData.Infrastructure.ReadModels;

namespace SignalPulse.MarketData.Application.AI.Cache;

public interface IQuoteCache
{
    Task<QuoteReadModel?> GetAsync(string symbol, CancellationToken ct = default);
    Task SetAsync(string symbol, QuoteReadModel value, CancellationToken ct = default);
}