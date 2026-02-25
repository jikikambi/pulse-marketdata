namespace MarketData.Adapter.Shared.Options;

public sealed class AlphaVantageOptions
{
    public string ApiKey { get; init; } = default!;

    /// <summary>
    /// Symbols to poll (AAPL, MSFT, BTCUSD, etc.)
    /// </summary>
    public IReadOnlyList<string> Symbols { get; init; } = [];
    public string BaseAddress { get; set; } = default!;
    public bool UseLive { get; set; } = true;
}
