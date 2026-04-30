namespace SignalPulse.MarketData.Application.AI.Models;

public sealed record PlannerResult(bool NeedTool, string Tool, double Confidence, string Reason);
