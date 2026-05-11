using SignalPulse.MarketData.Application.AI.Models;

namespace SignalPulse.MarketData.Application.AI.Plugins;

public interface IQuoteInfoTool
{
    Task<QuoteContextResult?> GetQuoteContextAsync(string symbol);
}
