using Refit;
using System.Diagnostics.CodeAnalysis;

namespace MarketData.Adapter.Shared.AlphaVantage.Request;

[ExcludeFromCodeCoverage(Justification = "DTO, no logic")]
public record AlphaVantageForexDailyRequest 
{
    [AliasAs("function")]
    public required string Function { get; set; } = "FX_DAILY";

    [AliasAs("from_symbol")]
    public required string FromSymbol { get; set; } = default!;

    [AliasAs("to_symbol")]
    public required string ToSymbol { get; set; } = default!;

    [AliasAs("apikey")]
    public required string Apikey { get; set; } = default!;
}