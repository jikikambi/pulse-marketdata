namespace SignalPulse.MarketData.Application.AI.Models;

public sealed record RecoverySummary(int TotalRecoveries, int TotalFailures, bool DegradedMode, IReadOnlyList<RecoveryEvent> Recoveries);