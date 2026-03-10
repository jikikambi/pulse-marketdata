using FluentAssertions;
using MarketData.Adapter.Shared.AlphaVantage.Response;
using MarketData.Adapter.Shared.Mappers;
using Refit;
using SignalPulse.Shared.UnitTests.Refit;
using System.Globalization;

namespace MarketData.Adapter.Handler.UnitTests.Mappers;

public class AlphaVantageQuoteMapperTests
{
    private readonly AlphaVantageQuoteMapper _mapper;

    public AlphaVantageQuoteMapperTests()
    {
        _mapper = new AlphaVantageQuoteMapper();
    }

    [Fact]
    public void MapTo_ShouldReturnNull_WhenApiResponseIsNotSuccess()
    {
        // Arrange
        var apiResponse = new ApiResponse<AlphaVantageQuoteResponse>(new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest),
            content: null!, settings: new RefitSettings());

        // Act
        var result = _mapper.MapTo(apiResponse);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void MapTo_ShouldReturnNull_WhenQuoteIsNullOrSymbolEmpty()
    {
        // Quote null
        var apiResponseNull = RefitTestsHelper.CreateOkResponseMock(new AlphaVantageQuoteResponse { Quote = null! });

        // Symbol empty
        var apiResponseEmptySymbol = RefitTestsHelper.CreateOkResponseMock(new AlphaVantageQuoteResponse
        {
            Quote = new AlphaVantageQuote { Symbol = " " }
        });

        // Act
        var resultNull = _mapper.MapTo(apiResponseNull);
        var resultEmpty = _mapper.MapTo(apiResponseEmptySymbol);

        // Assert
        resultNull.Should().BeNull();
        resultEmpty.Should().BeNull();
    }

    [Fact]
    public void MapTo_ShouldMapAllPropertiesCorrectly()
    {
        // Arrange
        var quote = new AlphaVantageQuote
        {
            Symbol = "MSFT",
            Open = "300.0",
            High = "305.0",
            Low = "295.0",
            Price = "301.0",
            PreviousClose = "300.0",
            Change = "1.0",
            ChangePercent = "0.33%",
            Volume = "1000000",
            LatestTradingDay = "2024-01-01"
        };

        var apiResponse = RefitTestsHelper.CreateOkResponseMock(new AlphaVantageQuoteResponse { Quote = quote });

        // Act
        var result = _mapper.MapTo(apiResponse);

        // Assert
        result.Should().NotBeNull();
        result!.Provider.Should().Be("AlphaVantage");
        result.Symbol.Should().Be("MSFT");
        result.Price.Should().Be(301.0m);
        result.Open.Should().Be(300.0m);
        result.High.Should().Be(305.0m);
        result.Low.Should().Be(295.0m);
        result.PreviousClose.Should().Be(300.0m);
        result.Change.Should().Be(1.0m);
        result.ChangePercent.Should().Be(0.0033m);
        result.Volume.Should().Be(1000000);
        result.LatestTradingDay.Should().Be(
            DateTime.Parse("2024-01-01", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)
        );
    }
}