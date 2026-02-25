namespace SignalPulse.Rdm.MarketData.AlphaVantage;

public record AlphaVantageQuoteRdm(Guid MessageId,
    string Provider,
    string Symbol,

    decimal Open,
    decimal High,
    decimal Low,
    decimal Price,
    decimal PreviousClose,
    decimal Change,
    decimal ChangePercent,

    long Volume,
    DateTime LatestTradingDay,
    DateTime ObservedAtUtc);
