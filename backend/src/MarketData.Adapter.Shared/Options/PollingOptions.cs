namespace MarketData.Adapter.Shared.Options;

public sealed class PollingOptions
{
    public const string SectionName = "Polling";

    /// <summary>
    /// Fixed polling interval (used by PeriodicTimer)
    /// </summary>
    public TimeSpan Interval { get; init; }

    /// <summary>
    /// Optional jitter to avoid thundering herd (recommended)
    /// </summary>
    public TimeSpan? Jitter { get; init; }
}