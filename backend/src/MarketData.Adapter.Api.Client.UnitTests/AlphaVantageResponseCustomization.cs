using AutoFixture;
using MarketData.Adapter.Shared.AlphaVantage.Response;

namespace MarketData.Adapter.Api.Client.UnitTests;

public class AlphaVantageResponseCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<AlphaVantageQuote>(composer =>
            composer
                .With(x => x.Symbol, "MSFT")
                .With(x => x.Price, "123.45")
                .With(x => x.Open, "120.00")
                .With(x => x.High, "125.00")
                .With(x => x.Low, "119.00")
                .With(x => x.Volume, "1000000")
                .With(x => x.LatestTradingDay, "2024-01-01")
                .With(x => x.PreviousClose, "121.00")
                .With(x => x.Change, "2.45")
                .With(x => x.ChangePercent, "2.03%")
        );

        fixture.Customize<AlphaVantageQuoteResponse>(composer =>
            composer.With(x => x.Quote, fixture.Create<AlphaVantageQuote>())
        );
    }
}