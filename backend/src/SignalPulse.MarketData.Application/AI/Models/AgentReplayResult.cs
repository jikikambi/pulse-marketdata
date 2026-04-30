namespace SignalPulse.MarketData.Application.AI.Models;

public record AgentReplayResult(string Key, List<string> Timeline, bool Completed);