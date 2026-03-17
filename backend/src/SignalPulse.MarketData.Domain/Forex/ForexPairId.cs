using SignalPulse.MarketData.Domain.Common;

namespace SignalPulse.MarketData.Domain.Forex;

public static class ForexPairId
{
    public static Guid From(string fromSymbol, string toSymbol) => DeterministicGuid.From($"FOREX:{fromSymbol}{toSymbol}");
}