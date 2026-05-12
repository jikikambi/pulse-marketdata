namespace SignalPulse.MarketData.Application.AI.Models;

public sealed record StageExecutionResult(string Stage, bool Success, long DurationMs, string? Error, DateTimeOffset StartedAt, DateTimeOffset CompletedAt);