using AutoFixture;
using FluentAssertions;
using MarketData.Adapter.Shared.AlphaVantage.Response;

namespace MarketData.Adapter.Api.Client.UnitTests;

public class AlphaVantageResponseObjectTests
{
    private readonly Fixture _fixture;

    public AlphaVantageResponseObjectTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AlphaVantageResponseCustomization());
    }

    [Fact]
    public void Fixture_ShouldCreate_ValidQuoteResponse()
    {
        // Arrange
        var response = _fixture.Create<AlphaVantageQuoteResponse>();

        // Assert
        response.Should().NotBeNull();
        response.Quote.Should().NotBeNull();
        response.Quote.Symbol.Should().Be("MSFT");
        response.Quote.Price.Should().Be("123.45");
    }
}