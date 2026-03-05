using AutoFixture;
using MarketData.Adapter.Shared.AlphaVantage.Request;
using MarketData.Adapter.Shared.AlphaVantage.Response;

namespace MarketData.Adapter.Api.Client.UnitTests;

public class AlphaVantageCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<AlphaVantageQuoteRequest>(composer =>
            composer.With(x => x.Function, "GLOBAL_QUOTE")
                    .With(x => x.Symbol, "MSFT")
                    .With(x => x.Apikey, "demo-key")
        );

        fixture.Customize<AlphaVantageQuote>(composer =>
            composer.With(x => x.Symbol, "MSFT")
                    .With(x => x.Price, "123.45")
        );
    }
}