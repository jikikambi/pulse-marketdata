using System.Text.Json.Serialization;

namespace MarketData.Adapter.Shared.AlphaVantage.Response;

public class AlphaVantageForexDaily
{
    [JsonPropertyName("1. Information")]
    public string Information { get; set; } = default!;

    [JsonPropertyName("2. From Symbol")]
    public string FromSymbol { get; set; } = default!;

    [JsonPropertyName("3. To Symbol")]
    public string ToSymbol { get; set; } = default!;

    [JsonPropertyName("4. Output Size")]
    public string OutputSize { get; set; } = default!;

    [JsonPropertyName("5. Last Refreshed")]
    public string LastRefreshed { get; set; } = default!;

    [JsonPropertyName("6. Time Zone")]
    public string TimeZone { get; set; } = default!;
}
