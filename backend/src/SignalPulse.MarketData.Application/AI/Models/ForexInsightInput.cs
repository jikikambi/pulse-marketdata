namespace SignalPulse.MarketData.Application.AI.Models;

public record ForexInsightInput(string FromSymbol, string ToSymbol, decimal Open, decimal High, decimal Low, decimal Close, DateTimeOffset ForexDate);