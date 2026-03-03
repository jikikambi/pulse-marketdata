namespace SignalPulse.MarketData.Infrastructure.ReadModels;

public class QuoteReadModel
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = default!;
    public decimal Price { get; set; }
    public decimal ChangePercent { get; set; }
    public DateTime Timestamp { get; set; }
}