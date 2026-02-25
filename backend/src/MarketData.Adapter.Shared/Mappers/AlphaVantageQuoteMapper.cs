using MarketData.Adapter.Shared.AlphaVantage.Response;
using MarketData.Adapter.Shared.Util;
using Refit;
using SignalPulse.Rdm.MarketData.AlphaVantage;
using System.Globalization;

namespace MarketData.Adapter.Shared.Mappers;

public class AlphaVantageQuoteMapper : IAlphaVantageQuoteMapper
{
    public AlphaVantageQuoteRdm? MapTo(ApiResponse<AlphaVantageQuoteResponse> apiResponse)
    {
        if (!apiResponse.IsSuccessStatusCode)
            return null;

        var quote = apiResponse.Content?.Quote;
        if (quote is null || string.IsNullOrWhiteSpace(quote.Symbol))
            return null;

        var culture = CultureInfo.InvariantCulture;

        var price = decimal.Parse(quote.Price, culture);
        var tradingDay = DateTime.Parse(quote.LatestTradingDay, culture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        var messageId = GuidUtility.Create(quote.Symbol, tradingDay, price, "AlphaVantage");

        return new AlphaVantageQuoteRdm(MessageId: messageId,
            Provider: "AlphaVantage",
            Symbol: quote.Symbol,

            Open: decimal.Parse(quote.Open, culture),
            High: decimal.Parse(quote.High, culture),
            Low: decimal.Parse(quote.Low, culture),
            Price: price,
            PreviousClose: decimal.Parse(quote.PreviousClose, culture),
            Change: decimal.Parse(quote.Change, culture),
            ChangePercent: ParsePercent(quote.ChangePercent),

            Volume: long.Parse(quote.Volume, culture),
            LatestTradingDay: tradingDay,
            ObservedAtUtc: DateTime.UtcNow);
    }

    private static decimal ParsePercent(string value)
    {
        // "-0.8335%" → -0.008335
        var trimmed = value.Replace("%", string.Empty);
        return decimal.Parse(trimmed, CultureInfo.InvariantCulture) / 100m;
    }
}