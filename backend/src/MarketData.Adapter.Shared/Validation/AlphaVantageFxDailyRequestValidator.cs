using FluentValidation;
using MarketData.Adapter.Shared.AlphaVantage.Request;

namespace MarketData.Adapter.Shared.Validation;

public class AlphaVantageFxDailyRequestValidator : AbstractValidator<AlphaVantageForexDailyRequest>
{
    public AlphaVantageFxDailyRequestValidator()
    {
        RuleFor(x => x.Function)
            .NotEmpty().WithMessage("Function is required.");

        RuleFor(x => x.FromSymbol)
            .NotEmpty().WithMessage("FromSymbol is required.");

        RuleFor(x => x.ToSymbol)
            .NotEmpty().WithMessage("ToSymbol is required.");

        RuleFor(x => x.Apikey)
            .NotEmpty().WithMessage("Apikey is required.");
    }
}