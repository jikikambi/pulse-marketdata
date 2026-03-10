
namespace SignalPulse.Common;

public static class SignalPulseConstants
{
    public const string RedisConnection = "localhost:6379";
    public const string MqConnection = "amqp://pulse:pulse@localhost:5672";
    public const string MqExchange = "marketdata";
}
