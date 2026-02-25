using SignalPulse.MarketData.Domain.Common;

namespace SignalPulse.MarketData.Domain.Quotes;

public static class QuoteId
{
    public static Guid From(string symbol) => DeterministicGuid.From($"QUOTE:{symbol}");
}
