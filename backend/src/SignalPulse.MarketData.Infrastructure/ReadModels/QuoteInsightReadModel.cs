namespace SignalPulse.MarketData.Infrastructure.ReadModels;

public class QuoteInsightReadModel
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = default!;
    public decimal Price { get; set; }
    public string Sentiment { get; set; } = default!;
    public string Direction { get; set; } = default!;
    public string Volatility { get; set; } = default!;
    public string Rationale { get; set; } = default!;
    public DateTimeOffset ObservedAt { get; set; }
}
