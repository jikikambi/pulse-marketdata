using FluentAssertions;
using MarketData.Adapter.Shared.AlphaVantage.Response;
using System.Text.Json;

namespace MarketData.Adapter.Api.Client.UnitTests;

public class AlphaVantageQuoteResponseDeserializationTests
{
    private const string Json = """
    {
      "Global Quote": {
        "01. symbol": "MSFT",
        "02. open": "120.00",
        "03. high": "125.00",
        "04. low": "119.00",
        "05. price": "123.45",
        "06. volume": "1000000",
        "07. latest trading day": "2024-01-01",
        "08. previous close": "121.00",
        "09. change": "2.45",
        "10. change percent": "2.03%"
      }
    }
    """;

    [Fact]
    public void Should_Deserialize_AlphaVantageQuoteResponse()
    {
        // Arrange
        var response = JsonSerializer.Deserialize<AlphaVantageQuoteResponse>(Json);

        // Assert
        response.Should().NotBeNull();
        response!.Quote.Should().NotBeNull();

        response.Quote.Symbol.Should().Be("MSFT");
        response.Quote.Price.Should().Be("123.45");
        response.Quote.ChangePercent.Should().Be("2.03%");
    }
}