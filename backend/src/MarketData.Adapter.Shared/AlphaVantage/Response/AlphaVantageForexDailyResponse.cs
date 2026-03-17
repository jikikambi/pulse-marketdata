using System.Text.Json.Serialization;

namespace MarketData.Adapter.Shared.AlphaVantage.Response;

public class AlphaVantageForexDailyResponse
{
    [JsonPropertyName("Meta Data")]
    public AlphaVantageForexDaily MetaData { get; set; } = default!;

    [JsonPropertyName("Time Series FX (Daily)")]
    public Dictionary<string, AlphaVantageForexDailyRate> TimeSeries { get; set; } = default!;
}
