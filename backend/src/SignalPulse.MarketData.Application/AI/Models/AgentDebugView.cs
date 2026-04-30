namespace SignalPulse.MarketData.Application.AI.Models;

public record AgentDebugView(string Key, Guid CorrelationId, string Symbol, string? Plan, bool ToolUsed, bool Completed, IReadOnlyList<AgentStep> Steps);