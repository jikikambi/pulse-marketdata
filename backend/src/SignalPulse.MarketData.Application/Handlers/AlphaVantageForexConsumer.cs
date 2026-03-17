using MassTransit;
using Microsoft.Extensions.Logging;
using SignalPulse.Rdm.MarketData.AlphaVantage;
using Wolverine;

namespace SignalPulse.MarketData.Application.Handlers;

public sealed class AlphaVantageForexConsumer(IMessageBus bus, ILogger<AlphaVantageForexConsumer> logger) : IConsumer<AlphaVantageForexRdm>
{
    public async Task Consume(ConsumeContext<AlphaVantageForexRdm> context)
    {
        var quoteRdm = context.Message;
        var ct = context.CancellationToken;

        logger.LogInformation(" {Consumer} Received {From}-{To}", nameof(AlphaVantageForexConsumer), quoteRdm.FromSymbol, quoteRdm.ToSymbol);

        await HandleAsync(quoteRdm, ct);
    }

    private async Task HandleAsync(AlphaVantageForexRdm quoteRdm, CancellationToken ct)
    {
        await bus.InvokeAsync(quoteRdm, ct);
    }
}
