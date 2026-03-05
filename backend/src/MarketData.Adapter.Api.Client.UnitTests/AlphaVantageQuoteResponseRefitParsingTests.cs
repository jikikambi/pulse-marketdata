using FluentAssertions;
using MarketData.Adapter.Shared.AlphaVantage.Response;
using SignalPulse.Shared.UnitTests.Refit;

namespace MarketData.Adapter.Api.Client.UnitTests;

public class AlphaVantageQuoteResponseRefitParsingTests
{
    [Fact]
    public void Deserialize_ValidAlphaVantageJson_ParsesCorrectly()
    {
        // Arrange
        var rawJson = """
        {
          "Global Quote": {
            "01. symbol": "MSFT",
            "05. price": "336.3200"
          }
        }
        """;

        var response = RefitTestsHelper.CreateOkJsonResponseMock<AlphaVantageQuoteResponse>(rawJson);

        // Act
        var model = response.Content;

        // Assert
        model.Should().NotBeNull();
        model!.Quote.Should().NotBeNull();
        model.Quote!.Symbol.Should().Be("MSFT");
        model.Quote.Price.Should().Be("336.3200");
    }
}