

using AutoFixture;
using MarketData.Adapter.Shared.AlphaVantage.Request;
using MarketData.Adapter.Shared.Validation;
using SignalPulse.Shared.UnitTests.FluentAssertions;

namespace MarketData.Adapter.Api.Client.UnitTests;

public class AlphaVantageQuoteRequestValidatorTests
    : ValidatorTestsBase<AlphaVantageQuoteRequestValidator, AlphaVantageQuoteRequest>
{
    public AlphaVantageQuoteRequestValidatorTests()
    {
        Fixture.Customize(new AlphaVantageCustomization());

        // Prevent AutoFixture recursion issues (common with records)
        Fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => Fixture.Behaviors.Remove(b));

        Fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public void Validate_ValidRequest_NoValidationErrors()
    {
        // Arrange
        var req = Fixture.Create<AlphaVantageQuoteRequest>();

        // Act & Assert
        ValidateValidMessage(req);
    }

    [Fact]
    public void ValidRequest_ShouldPass()
    {
        // Arrange
        var req = Fixture.Create<AlphaVantageQuoteRequest>();

        // Act & Assert
        ShouldBeValid(req);
    }

    [Fact]
    public void MissingSymbol_ShouldFail()
    {
        // Arrange
        var req = Fixture.Create<AlphaVantageQuoteRequest>();
        req.Symbol = null!;

        // Act & Assert
        ShouldHaveError(req, "Symbol is required.");
    }

    [Fact]
    public void MissingApikey_ShouldFail()
    {
        // Arrange
        var req = Fixture.Create<AlphaVantageQuoteRequest>();
        req.Apikey = null!;

        // Act & Assert
        ShouldHaveError(req, "Apikey is required.");
    }

    [Fact]
    public void MissingFunction_ShouldFail()
    {
        // Arrange
        var req = Fixture.Create<AlphaVantageQuoteRequest>();
        req.Function = null!;

        // Act & Assert
        ShouldHaveError(req, "Function is required.");
    }

    [Fact]
    public void ValidateRule_WithMutationFunc_ShouldFail()
    {
        // Act & Assert
        ValidateRule(msg => msg.Symbol = "", "Symbol is required.");
    }
}