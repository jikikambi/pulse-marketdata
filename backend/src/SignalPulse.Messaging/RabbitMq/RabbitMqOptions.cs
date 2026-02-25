namespace SignalPulse.Messaging.RabbitMq;

public sealed class RabbitMqOptions
{
    public string MqConnection { get; set; } = "amqp://pulse:pulse@localhost:5672";
    public string Exchange { get; set; } = "marketdata";
}