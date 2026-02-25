namespace MarketData.Adapter.Messaging.Events;

/// <summary>
/// 
/// </summary>
/// <param name="MessageId">idempotency on consumer side</param>
/// <param name="Provider">AlphaVantage / CryptoAPI</param>
/// <param name="Symbol"></param>
/// <param name="Price"></param>
/// <param name="Timestamp"></param>
public record ExternalQuoteReceived(Guid MessageId,string Provider,string Symbol, decimal Price, DateTime Timestamp);

public record AlphaVantageQuoteMessage(Guid MessageId,
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
    DateTime ObservedAtUtc
    );