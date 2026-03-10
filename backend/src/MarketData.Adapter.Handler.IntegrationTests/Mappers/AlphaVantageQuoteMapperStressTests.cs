using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using FluentAssertions;
using MarketData.Adapter.Shared.AlphaVantage.Response;
using MarketData.Adapter.Shared.Mappers;
using SignalPulse.Shared.UnitTests.Refit;
using System.Globalization;

namespace MarketData.Adapter.Handler.IntegrationTests.Mappers;

public class AlphaVantageQuoteMapperStressTests
{
    private readonly IFixture _fixture;
    private readonly AlphaVantageQuoteMapper _mapper;

    public AlphaVantageQuoteMapperStressTests()
    {
        _fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
        
        _fixture.Customize<AlphaVantageQuote>(c => c
            .With(q => q.Symbol, () => _fixture.Create<string>().Substring(0, 4))
            .With(q => q.Open, () => _fixture.Create<decimal>().ToString(CultureInfo.InvariantCulture))
            .With(q => q.High, () => _fixture.Create<decimal>().ToString(CultureInfo.InvariantCulture))
            .With(q => q.Low, () => _fixture.Create<decimal>().ToString(CultureInfo.InvariantCulture))
            .With(q => q.Price, () => _fixture.Create<decimal>().ToString(CultureInfo.InvariantCulture))
            .With(q => q.PreviousClose, () => _fixture.Create<decimal>().ToString(CultureInfo.InvariantCulture))
            .With(q => q.Change, () => _fixture.Create<decimal>().ToString(CultureInfo.InvariantCulture))
            .With(q => q.ChangePercent, () => (_fixture.Create<decimal>() % 100).ToString("0.##", CultureInfo.InvariantCulture) + "%")
            .With(q => q.Volume, () => _fixture.Create<long>().ToString(CultureInfo.InvariantCulture))
            .With(q => q.LatestTradingDay, () => _fixture.Create<DateTime>().ToString("yyyy-MM-dd"))
        );

        _mapper = new AlphaVantageQuoteMapper();
    }

    [Fact]
    public void MapTo_ShouldHandleMultipleRandomQuotes()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            var quote = _fixture.Create<AlphaVantageQuote>();
            var apiResponse = RefitTestsHelper.CreateOkResponseMock(new AlphaVantageQuoteResponse { Quote = quote });

            // Act
            var result = _mapper.MapTo(apiResponse);

            // Assert
            result.Should().NotBeNull();
            result!.Symbol.Should().Be(quote.Symbol);
            result.Price.Should().Be(decimal.Parse(quote.Price, CultureInfo.InvariantCulture));
            result.Open.Should().Be(decimal.Parse(quote.Open, CultureInfo.InvariantCulture));
            result.High.Should().Be(decimal.Parse(quote.High, CultureInfo.InvariantCulture));
            result.Low.Should().Be(decimal.Parse(quote.Low, CultureInfo.InvariantCulture));
            result.PreviousClose.Should().Be(decimal.Parse(quote.PreviousClose, CultureInfo.InvariantCulture));
            result.Change.Should().Be(decimal.Parse(quote.Change, CultureInfo.InvariantCulture));

            var expectedPercent = decimal.Parse(quote.ChangePercent.Replace("%", ""), CultureInfo.InvariantCulture) / 100m;
            result.ChangePercent.Should().Be(expectedPercent);

            result.Volume.Should().Be(long.Parse(quote.Volume, CultureInfo.InvariantCulture));

            var expectedDate = DateTime.Parse(quote.LatestTradingDay, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            result.LatestTradingDay.Should().Be(expectedDate);
        }
    }
}