using SignalPulse.MarketData.Application.AI;
using SignalPulse.MarketData.Application.Interfaces;

namespace SignalPulse.MarketData.Application.Services;

public partial class SemanticKernelInsightProvider
{
    public class MockInsightProvider : IAiInsightProvider
    {
        private static readonly Random _rand = new();

        public async Task<AIInsightResult> GenerateAsync(string symbol,
            decimal price,
            decimal changePercent,
            long volume, 
            CancellationToken ct = default)
        {
            // Fake but realistic logic
            var directions = new[] { "up", "down", "sideways" };
            var sentiments = new[] { "bullish", "bearish", "neutral" };
            var volLevels = new[] { "low", "medium", "high" };

            var direction = directions[_rand.Next(directions.Length)];
            var sentiment = sentiments[_rand.Next(sentiments.Length)];
            var volLevel = volLevels[_rand.Next(volLevels.Length)];

            var explanation =
                $"{symbol} is trending {direction} with a {sentiment} sentiment. " +
                $"Current volume is {volLevel} relative to recent activity. " +
                $"Price: {price:C}, Daily Change: {changePercent:+0.##;-0.##;0}%.";

            return await Task.FromResult(new AIInsightResult(sentiment, direction, volLevel, explanation));
        }
    }
}