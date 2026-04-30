namespace SignalPulse.MarketData.Application.AI.Models;

public sealed record AgentStepView(DateTimeOffset Timestamp, string StepName, string Input, string Output);