using Microsoft.Extensions.Logging;
using SignalPulse.MarketData.Application.Common;
using SignalPulse.Rdm.MarketData.AlphaVantage;
using Wolverine;

namespace SignalPulse.MarketData.Application.Handlers;

public sealed class AlphaVantageQuoteConsumer(IMessageBus bus, ILogger<AlphaVantageQuoteConsumer> logger) : AlphaVantageConsumerBase<AlphaVantageQuoteRdm>(bus, logger)
{
    protected override void LogReceived(AlphaVantageQuoteRdm message) => 
        Logger.LogInformation(" {Consumer} Received {Symbol}", nameof(AlphaVantageQuoteConsumer), message.Symbol);
}