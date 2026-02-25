namespace MarketData.Adapter.Shared.Options;

public static class Jitter
{
    public static TimeSpan Next(TimeSpan max)
    {
        if (max <= TimeSpan.Zero)
            return TimeSpan.Zero;

        var millis = Random.Shared.NextDouble() * max.TotalMilliseconds;
        return TimeSpan.FromMilliseconds(millis);
    }
}