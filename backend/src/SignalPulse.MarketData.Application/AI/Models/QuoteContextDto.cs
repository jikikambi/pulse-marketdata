namespace SignalPulse.MarketData.Application.AI.Models;

public sealed class QuoteContextDto
{
    public decimal Price { get; set; }
    public decimal ChangePercent { get; set; }
    public decimal? AvgPrice { get; set; }
    public decimal? Max { get; set; }
    public decimal? Min { get; set; }
    public string Source { get; set; } = default!;
}