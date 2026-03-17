namespace MarketData.Adapter.Shared.Options;

public sealed class AlphaVantageOptions
{
    /// <summary>
    /// Symbols to poll (AAPL, MSFT, BTCUSD, etc.)
    /// </summary>
    public IReadOnlyList<string> QuoteSymbols { get; init; } = [];

    /// <summary>
    /// Symbols to poll ("EUR", "USD", "GBP", "JPY", "CHF", etc.)
    /// </summary>
    public IReadOnlyList<(string From, string To)> ForexSymbols { get; init; } = [];
    public string BaseAddress { get; set; } = default!;
    public bool UseLive { get; set; } = true;
}
