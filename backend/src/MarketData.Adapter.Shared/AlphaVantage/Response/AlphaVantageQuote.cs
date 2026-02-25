using System.Text.Json.Serialization;

namespace MarketData.Adapter.Shared.AlphaVantage.Response;

public sealed class AlphaVantageQuote
{
    [JsonPropertyName("01. symbol")]
    public string Symbol { get; set; } = default!;

    [JsonPropertyName("02. open")]
    public string Open { get; set; } = default!;

    [JsonPropertyName("03. high")]
    public string High { get; set; } = default!;

    [JsonPropertyName("04. low")]
    public string Low { get; set; } = default!;

    [JsonPropertyName("05. price")]
    public string Price { get; set; } = default!;

    [JsonPropertyName("06. volume")]
    public string Volume { get; set; } = default!;

    [JsonPropertyName("07. latest trading day")]
    public string LatestTradingDay { get; set; } = default!;

    [JsonPropertyName("08. previous close")]
    public string PreviousClose { get; set; } = default!;

    [JsonPropertyName("09. change")]
    public string Change { get; set; } = default!;

    [JsonPropertyName("10. change percent")]
    public string ChangePercent { get; set; } = default!;
}
