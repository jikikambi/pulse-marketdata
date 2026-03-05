using FluentValidation;
using MarketData.Adapter.Shared.AlphaVantage.Request;

namespace MarketData.Adapter.Shared.Validation;

public class AlphaVantageQuoteRequestValidator : AbstractValidator<AlphaVantageQuoteRequest>
{
    public AlphaVantageQuoteRequestValidator()
    {
        RuleFor(x => x.Function)
            .NotEmpty().WithMessage("Function is required.");

        RuleFor(x => x.Symbol)
            .NotEmpty().WithMessage("Symbol is required.");

        RuleFor(x => x.Apikey)
            .NotEmpty().WithMessage("Apikey is required.");
    }
}
