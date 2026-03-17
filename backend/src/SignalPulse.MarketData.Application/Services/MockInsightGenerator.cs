namespace SignalPulse.MarketData.Application.Services;

internal static class MockInsightGenerator
{
    private static readonly Random Rand = new();

    public static (string sentiment, string direction, string volatility) Generate()
    {
        var directions = new[] { "up", "down", "sideways" };
        var sentiments = new[] { "bullish", "bearish", "neutral" };
        var volLevels = new[] { "low", "medium", "high" };

        return (
            sentiments[Rand.Next(sentiments.Length)],
            directions[Rand.Next(directions.Length)],
            volLevels[Rand.Next(volLevels.Length)]
        );
    }
}