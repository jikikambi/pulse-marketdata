namespace SignalPulse.MarketData.Application.AI.Models;

public record AgentStep(string StepName, string Input, string Output, DateTimeOffset Timestamp, string? Metadata = null);