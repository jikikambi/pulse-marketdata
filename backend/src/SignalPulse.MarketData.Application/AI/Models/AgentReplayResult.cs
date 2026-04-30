namespace SignalPulse.MarketData.Application.AI.Models;

public record AgentReplayResult(string Key, List<AgentStepView> Timeline, bool Completed);
