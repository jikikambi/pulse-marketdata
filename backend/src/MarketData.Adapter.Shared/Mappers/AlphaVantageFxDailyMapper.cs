using MarketData.Adapter.Shared.AlphaVantage.Response;
using MarketData.Adapter.Shared.Util;
using Refit;
using SignalPulse.Rdm.MarketData.AlphaVantage;

namespace MarketData.Adapter.Shared.Mappers;

using System.Globalization;

public class AlphaVantageForexDailyMapper : IAlphaVantageForexDailyMapper
{
    public IEnumerable<AlphaVantageForexRdm>? MapTo(ApiResponse<AlphaVantageForexDailyResponse> apiResponse)
    {
        if (!apiResponse.IsSuccessStatusCode)
            return null;

        var meta = apiResponse.Content?.MetaData;
        if (meta is null ||
            string.IsNullOrWhiteSpace(meta.FromSymbol) ||
            string.IsNullOrWhiteSpace(meta.ToSymbol))
            return null;

        var culture = CultureInfo.InvariantCulture;
        var provider = "AlphaVantage";
        var symbol = $"{meta.FromSymbol}{meta.ToSymbol}";

        var lastRefreshed = DateTimeOffset.Parse(
            meta.LastRefreshed,
            culture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        // Map each daily rate
        var rates = apiResponse.Content!.TimeSeries
            .Select(entry =>
            {
                var date = DateTimeOffset.Parse(
                    entry.Key,
                    culture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

                var open = decimal.Parse(entry.Value.Open, culture);
                var high = decimal.Parse(entry.Value.High, culture);
                var low = decimal.Parse(entry.Value.Low, culture);
                var close = decimal.Parse(entry.Value.Close, culture);

                // Deterministic ID per symbol + date + close price
                var messageId = GuidUtility.Create(symbol, date, close, provider);

                return new AlphaVantageForexRdm(
                    MessageId: messageId,
                    Provider: provider,
                    Symbol: symbol,
                    ForexDate: date, 
                    Information: meta.Information,
                    FromSymbol: meta.FromSymbol,
                    ToSymbol: meta.ToSymbol,
                    OutputSize: meta.OutputSize,
                    LastRefreshed: lastRefreshed,
                    TimeZone: meta.TimeZone,
                    Open: open,
                    High: high,
                    Low: low,
                    Close: close
                );
            })
            .OrderBy(x => x.ForexDate)
            .ToList();

        return rates;
    }
}