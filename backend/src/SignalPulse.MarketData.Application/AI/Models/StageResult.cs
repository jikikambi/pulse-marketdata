namespace SignalPulse.MarketData.Application.AI.Models;

public sealed record StageResult<T>(T Value, bool Continue, string? Reason = null);
