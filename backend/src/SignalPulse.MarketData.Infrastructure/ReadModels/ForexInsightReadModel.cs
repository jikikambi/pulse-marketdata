namespace SignalPulse.MarketData.Infrastructure.ReadModels;

public class ForexInsightReadModel
{
    public Guid Id { get; set; }
    public string FromSymbol { get; set; } = default!;
    public string ToSymbol { get; set; } = default!;
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public DateTimeOffset ForexDate { get; set; }
    public string Sentiment { get; set; } = default!;
    public string Direction { get; set; } = default!;
    public string Volatility { get; set; } = default!;
    public string Rationale { get; set; } = default!;
    public DateTimeOffset ObservedAt { get; set; }
}
