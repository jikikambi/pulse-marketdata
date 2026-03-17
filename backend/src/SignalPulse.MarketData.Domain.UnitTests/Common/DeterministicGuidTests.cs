
using FluentAssertions;
using SignalPulse.MarketData.Domain.Common;
using SignalPulse.MarketData.Domain.Forex;
using SignalPulse.MarketData.Domain.Quotes;

namespace SignalPulse.MarketData.Domain.UnitTests.Common;

public class DeterministicGuidTests
{
    [Fact]
    public void From_Should_Return_Same_Guid_For_Same_Input()
    {
        // Arrange
        var input = "EURUSD";

        // Act
        var guid1 = DeterministicGuid.From(input);
        var guid2 = DeterministicGuid.From(input);

        // Assert
        guid1.Should().Be(guid2, "the same input should always produce the same deterministic GUID");
    }

    [Fact]
    public void From_Should_Return_Different_Guids_For_Different_Inputs()
    {
        // Arrange
        var input1 = "EURUSD";
        var input2 = "USDJPY";

        // Act
        var guid1 = DeterministicGuid.From(input1);
        var guid2 = DeterministicGuid.From(input2);

        // Assert
        guid1.Should().NotBe(guid2, "different inputs should produce different GUIDs");
    }

    [Fact]
    public void From_Should_Return_Valid_Guid_Format()
    {
        // Arrange
        var input = "AnyString";

        // Act
        var guid = DeterministicGuid.From(input);

        // Assert
        guid.Should().NotBe(Guid.Empty, "MD5 hash of a string should never produce an empty GUID");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void From_Should_Handle_EmptyOrNull_Input(string? input)
    {
        // Act
        var guid = DeterministicGuid.From(input ?? string.Empty);

        // Assert
        guid.Should().NotBe(Guid.Empty, "even empty strings should produce a valid deterministic GUID");
    }

    [Fact]
    public void ForexPairId_Should_Be_Deterministic_For_Same_Input()
    {
        // Arrange
        var from = "EUR";
        var to = "USD";

        // Act
        var id1 = ForexPairId.From(from, to);
        var id2 = ForexPairId.From(from, to);

        // Assert
        id1.Should().Be(id2, "the same Forex pair should always produce the same deterministic GUID");
    }

    [Fact]
    public void ForexPairId_Should_Be_Different_For_Different_Pairs()
    {
        // Arrange
        var id1 = ForexPairId.From("EUR", "USD");
        var id2 = ForexPairId.From("USD", "JPY");

        // Assert
        id1.Should().NotBe(id2, "different Forex pairs should produce different GUIDs");
    }

    [Fact]
    public void QuoteId_Should_Be_Deterministic_For_Same_Input()
    {
        // Arrange
        var symbol = "AAPL";

        // Act
        var id1 = QuoteId.From(symbol);
        var id2 = QuoteId.From(symbol);

        // Assert
        id1.Should().Be(id2, "the same quote symbol should always produce the same deterministic GUID");
    }

    [Fact]
    public void QuoteId_Should_Be_Different_For_Different_Symbols()
    {
        // Arrange
        var id1 = QuoteId.From("AAPL");
        var id2 = QuoteId.From("MSFT");

        // Assert
        id1.Should().NotBe(id2, "different quote symbols should produce different GUIDs");
    }

    [Fact]
    public void ForexPairId_And_QuoteId_Should_Not_Produce_Collisions()
    {
        // Arrange
        var forexId = ForexPairId.From("EUR", "USD");
        var quoteId = QuoteId.From("EURUSD");

        // Assert
        forexId.Should().NotBe(quoteId, "Forex pair IDs and Quote IDs must be distinct even with similar strings");
    }

    [Theory]
    [InlineData("", "USD")]
    [InlineData("EUR", "")]
    [InlineData("", "")]
    public void ForexPairId_Should_Handle_Empty_Strings(string from, string to)
    {
        // Act
        var id = ForexPairId.From(from, to);

        // Assert
        id.Should().NotBe(Guid.Empty, "even empty symbols should produce a valid deterministic GUID");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void QuoteId_Should_Handle_Empty_Or_Null(string? symbol)
    {
        // Act
        var id = QuoteId.From(symbol ?? string.Empty);

        // Assert
        id.Should().NotBe(Guid.Empty, "even empty or null symbols should produce a valid deterministic GUID");
    }
}