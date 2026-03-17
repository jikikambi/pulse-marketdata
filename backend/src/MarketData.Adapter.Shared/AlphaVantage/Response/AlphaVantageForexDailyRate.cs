using System.Text.Json.Serialization;

namespace MarketData.Adapter.Shared.AlphaVantage.Response;

public class AlphaVantageForexDailyRate
{
    [JsonPropertyName("1. open")]
    public string Open { get; set; } = default!;

    [JsonPropertyName("2. high")]
    public string High { get; set; } = default!;

    [JsonPropertyName("3. low")]
    public string Low { get; set; } = default!;

    [JsonPropertyName("4. close")]
    public string Close { get; set; } = default!;
}