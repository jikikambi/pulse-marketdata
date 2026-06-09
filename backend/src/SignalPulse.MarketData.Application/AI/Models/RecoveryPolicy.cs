using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Models;

public sealed class RecoveryPolicy
{
    public FailureCategory Category { get; init; }

    public RecoveryStrategy Strategy { get; init; }

    public int MaxAttempts { get; init; }
}