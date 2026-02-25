namespace SignalPulse.Persistence.Redis;

public class IdempotencyOptions
{
    /// <summary>
    /// Default TTL for idempotency entries.
    /// Can be overridden per use-case if needed.
    /// </summary>
    public TimeSpan EntryTtl { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Global Redis key namespace for idempotency.
    /// Example keys:
    ///   idem:cmd:{guid}
    ///   idem:evt:{guid}
    /// </summary>
    public string KeyPrefix { get; set; } = "idem:";
}
