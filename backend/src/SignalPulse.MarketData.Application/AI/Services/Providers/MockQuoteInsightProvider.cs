using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.Interfaces;

namespace SignalPulse.MarketData.Application.AI.Services.Providers;

public class MockQuoteInsightProvider : IAiInsightProvider<QuoteInsightInput>
{
    public  Task<AIInsightResult> GenerateAsync(QuoteInsightInput input, CancellationToken ct = default)
    {
        var (sentiment, direction, volatility) = MockInsightGenerator.Generate();

        var explanation =
            $"{input.Symbol} is trending {direction} with a {sentiment} sentiment. " +
            $"Current volume is {volatility} relative to recent activity. " +
            $"Price: {input.Price:C}, Daily Change: {input.ChangePercent:+0.##;-0.##;0}%.";

        return Task.FromResult(new AIInsightResult(
           Enum.Parse<SentimentType>(sentiment, true),
           Enum.Parse<DirectionType>(direction, true),
           Enum.Parse<VolatilityType>(volatility, true),
           explanation));
    }
}