namespace SignalPulse.MarketData.Application.AI.Models.Enums;

public enum RecoveryStrategy
{
    Continue,
    None,
    Retry,
    Fallback,
    Skip,
    Degrade,
    Reroute,
    Terminate
}