using SignalPulse.Rdm.MarketData.AlphaVantage;

namespace SignalPulse.MarketData.Application.UnitTests.Handlers;

internal static class MarketDataRdmBuilder
{
    public static AlphaVantageForexRdm ValidForex(Func<AlphaVantageForexRdm, AlphaVantageForexRdm>? customize = null)
    {
        var now = DateTimeOffset.UtcNow;

        var rdm = new AlphaVantageForexRdm(
            Guid.NewGuid(),
            "AlphaVantage",
            "EURUSD",

            "Forex Daily Prices",
            "EUR",
            "USD",
            "Compact",
            now,
            "UTC",

            now,

            1.20m,
            1.30m,
            1.10m,
            1.25m
        );

        return customize?.Invoke(rdm) ?? rdm;
    }

    public static AlphaVantageForexRdm Invalid() =>
        ValidForex(x => x with
        {
            High = 1.0m,
            Low = 2.0m
        });

    public static AlphaVantageQuoteRdm ValidQuote(
        Func<AlphaVantageQuoteRdm, AlphaVantageQuoteRdm>? customize = null)
    {
        var now = DateTimeOffset.UtcNow;

        var rdm = new AlphaVantageQuoteRdm(
            MessageId: Guid.NewGuid(),
            Provider: "AlphaVantage",
            Symbol: "AAPL",

            Open: 150m,
            High: 155m,
            Low: 145m,
            Price: 152m,
            PreviousClose: 149m,
            Change: 3m,
            ChangePercent: 2.0m,

            Volume: 1_000_000,
            LatestTradingDay: now,
            ObservedAtUtc: now
        );

        return customize?.Invoke(rdm) ?? rdm;
    }

    public static AlphaVantageQuoteRdm InvalidQuote() =>
        ValidQuote(x => x with
        {
            Price = -1m // assuming invalid (domain rule)
        });
}