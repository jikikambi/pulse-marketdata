using SignalPulse.MarketData.Application.AI;
using SignalPulse.MarketData.Application.Interfaces;

namespace SignalPulse.MarketData.Application.Services;

public class MockQuoteInsightProvider : IAiInsightProvider<QuoteInsightInput>
{

    public  Task<AIInsightResult> GenerateAsync(QuoteInsightInput input, CancellationToken ct = default)
    {  
        var explanation =
            $"{input.Symbol} is trending {MockInsightGenerator.Generate().direction} with a {MockInsightGenerator.Generate().sentiment} sentiment. " +
            $"Current volume is {MockInsightGenerator.Generate().volatility} relative to recent activity. " +
            $"Price: {input.Price:C}, Daily Change: {input.ChangePercent:+0.##;-0.##;0}%.";

        return Task.FromResult(new AIInsightResult(MockInsightGenerator.Generate().sentiment, MockInsightGenerator.Generate().direction, MockInsightGenerator.Generate().volatility, explanation));
    }
}