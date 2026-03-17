using MassTransit;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace SignalPulse.MarketData.Application.Common;

public abstract class AlphaVantageConsumerBase<TMessage>(IMessageBus bus, ILogger logger) : IConsumer<TMessage>
    where TMessage : class
{
    protected readonly IMessageBus Bus = bus;
    protected readonly ILogger Logger = logger;

    public async Task Consume(ConsumeContext<TMessage> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        LogReceived(message);

        await HandleAsync(message, ct);
    }

    /// <summary>
    /// Allows derived classes to customize logging
    /// </summary>
    protected abstract void LogReceived(TMessage message);

    protected virtual async Task HandleAsync(TMessage message, CancellationToken ct)
    {
        await Bus.InvokeAsync(message, ct);
    }
}