using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Models;

public sealed record RecoveryEvent(MarketAgentStage FailedStage, RecoveryStrategy Strategy, string Reason, DateTimeOffset OccurredAt);