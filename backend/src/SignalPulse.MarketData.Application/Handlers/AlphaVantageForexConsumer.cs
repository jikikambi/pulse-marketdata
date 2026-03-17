using Microsoft.Extensions.Logging;
using SignalPulse.MarketData.Application.Common;
using SignalPulse.Rdm.MarketData.AlphaVantage;
using Wolverine;

namespace SignalPulse.MarketData.Application.Handlers;

public sealed class AlphaVantageForexConsumer(IMessageBus bus, ILogger<AlphaVantageForexConsumer> logger) : AlphaVantageConsumerBase<AlphaVantageForexRdm>(bus, logger)
{
    protected override void LogReceived(AlphaVantageForexRdm message) => 
        Logger.LogInformation(" {Consumer} Received {From}-{To}", nameof(AlphaVantageForexConsumer), message.FromSymbol, message.ToSymbol);
}