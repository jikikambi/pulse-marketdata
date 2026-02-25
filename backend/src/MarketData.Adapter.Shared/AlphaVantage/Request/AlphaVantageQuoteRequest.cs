using Refit;
using System.Diagnostics.CodeAnalysis;

namespace MarketData.Adapter.Shared.AlphaVantage.Request;

[ExcludeFromCodeCoverage(Justification = "DTO, no logic")]
public record AlphaVantageQuoteRequest
{
    [AliasAs("function")]
    public required string Function { get; set; } = "GLOBAL_QUOTE";

    [AliasAs("symbol")]
    public required string Symbol { get; set; } = default!;

    [AliasAs("apikey")]
    public required string Apikey { get; set; } = default!;
}