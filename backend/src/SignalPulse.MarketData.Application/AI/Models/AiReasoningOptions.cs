using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Models;

public sealed class AiReasoningOptions
{
    public ReasoningProvider Provider { get; set; }
    public bool EnableFallback { get; set; }
    public string? FallbackProvider { get; set; }
}