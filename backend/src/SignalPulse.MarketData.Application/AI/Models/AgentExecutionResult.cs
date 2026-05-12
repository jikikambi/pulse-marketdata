namespace SignalPulse.MarketData.Application.AI.Models;

public sealed record AgentExecutionResult<T>(bool Success, T? Data, string? FailureReason, TimeSpan Duration, string AgentName);