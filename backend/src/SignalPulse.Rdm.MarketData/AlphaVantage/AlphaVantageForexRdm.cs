namespace SignalPulse.Rdm.MarketData.AlphaVantage;

public record AlphaVantageForexRdm(Guid MessageId,
    string Provider,
    string Symbol,    

    string Information,
    string FromSymbol,
    string ToSymbol,
    string OutputSize,
    DateTimeOffset LastRefreshed,
    string TimeZone,

    DateTimeOffset ForexDate,

    decimal Open,
    decimal High,
    decimal Low,
    decimal Close);