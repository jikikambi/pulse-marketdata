using System.Text.Json.Serialization;

namespace MarketData.Adapter.Shared.AlphaVantage.Response;

public sealed class AlphaVantageQuoteResponse
{
    [JsonPropertyName("Global Quote")]
    public AlphaVantageQuote Quote { get; set; } = default!;
}