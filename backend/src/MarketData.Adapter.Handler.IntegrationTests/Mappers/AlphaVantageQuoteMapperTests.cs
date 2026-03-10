using FluentAssertions;
using MarketData.Adapter.Shared.AlphaVantage.Response;
using MarketData.Adapter.Shared.Mappers;
using SignalPulse.Shared.UnitTests.Refit;
using System.Globalization;

namespace MarketData.Adapter.Handler.IntegrationTests.Mappers;

public class AlphaVantageQuoteMapperTests
{
    private readonly AlphaVantageQuoteMapper _mapper = new();

    [Fact]
    public void MapTo_ShouldMapJsonResponseCorrectly()
    {
        // Arrange
        var json = """
        {
           "Global Quote": {
             "01. symbol": "AAPL",
             "02. open": "150.0",
             "03. high": "155.0",
             "04. low": "148.0",
             "05. price": "152.5",
             "06. volume": "2000000",
             "07. latest trading day": "2024-01-01",
             "08. previous close": "150.0",
             "09. change": "2.5",
             "10. change percent": "1.6667%"
           }
        }
        """;

        var apiResponse = RefitTestsHelper.CreateOkJsonResponseMock<AlphaVantageQuoteResponse>(json);

        // Act
        var rdm = _mapper.MapTo(apiResponse);

        // Assert
        rdm.Should().NotBeNull();
        rdm!.Provider.Should().Be("AlphaVantage");
        rdm.Symbol.Should().Be("AAPL");
        rdm.Price.Should().Be(152.5m);
        rdm.Open.Should().Be(150.0m);
        rdm.High.Should().Be(155.0m);
        rdm.Low.Should().Be(148.0m);
        rdm.PreviousClose.Should().Be(150.0m);
        rdm.Change.Should().Be(2.5m);
        rdm.ChangePercent.Should().BeApproximately(0.016667m, 0.000001m);
        rdm.Volume.Should().Be(2000000);
        rdm.LatestTradingDay.Should().Be(
            DateTime.Parse("2024-01-01", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)
        );
    }
}
