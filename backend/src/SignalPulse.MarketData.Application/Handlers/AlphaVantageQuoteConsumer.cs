using MassTransit;
using Microsoft.Extensions.Logging;
using SignalPulse.Rdm.MarketData.AlphaVantage;
using Wolverine;

namespace SignalPulse.MarketData.Application.Handlers;

public sealed class AlphaVantageQuoteConsumer(IMessageBus bus, ILogger<AlphaVantageQuoteConsumer> logger) : IConsumer<AlphaVantageQuoteRdm>
{
    public async Task Consume(ConsumeContext<AlphaVantageQuoteRdm> context)
    {
        var quoteRdm = context.Message;
        var ct = context.CancellationToken;

        logger.LogInformation(" [x] Received {Message}", quoteRdm.Symbol);

        await HandleAsync(quoteRdm, ct);
    }

    private async Task HandleAsync(AlphaVantageQuoteRdm quoteRdm, CancellationToken ct)
    {
        await bus.InvokeAsync(quoteRdm, ct);
    }
}
