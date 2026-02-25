namespace SignalPulse.MarketData.Infrastructure.RedisStore;

public static class IdempotencyKeys
{
    public static string Command(Guid id) => $"cmd:{id}";
    public static string Event(Guid id) => $"evt:{id}";
}