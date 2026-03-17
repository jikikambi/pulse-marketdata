namespace SignalPulse.MarketData.Application.AI;

public record ForexInsightInput(string FromSymbol, string ToSymbol, decimal Open, decimal High, decimal Low, decimal Close, DateTimeOffset ForexDate);