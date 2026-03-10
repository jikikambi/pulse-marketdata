using SignalPulse.Common;

namespace SignalPulse.Messaging.RabbitMq;

public sealed class RabbitMqOptions
{
    public string MqConnection { get; set; } = SignalPulseConstants.MqConnection;
    public string Exchange { get; set; } = SignalPulseConstants.MqExchange;
}