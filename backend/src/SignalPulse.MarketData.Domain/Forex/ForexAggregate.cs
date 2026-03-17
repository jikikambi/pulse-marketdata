using SignalPulse.MarketData.Contracts.Events;
using SignalPulse.MarketData.Domain.Common;
using SignalPulse.MarketData.Domain.Exceptions;

namespace SignalPulse.MarketData.Domain.Forex;

public sealed class ForexAggregate : AggregateRoot
{
    public string FromSymbol { get; private set; }
    public string ToSymbol { get; private set; }

    public decimal Open { get; private set; }
    public decimal High { get; private set; }
    public decimal Low { get; private set; }
    public decimal Close { get; private set; }
    public DateTimeOffset ForexDate { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>
    /// Parameterless constructor for ORM/reflection-based instantiation (e.g., event sourcing rehydration).
    /// Direct instantiation is discouraged; use Create() factory method instead.
    /// </summary>
    public ForexAggregate()
    {
        FromSymbol = string.Empty;
        ToSymbol = string.Empty;
    }

    public static ForexAggregate Create(string fromSymbol, string toSymbol, decimal open, decimal high, decimal low, decimal close, DateTimeOffset fxDate)
    {
        ValidateForexData(fromSymbol, toSymbol, open, high, low, close, fxDate);

        var aggregate = new ForexAggregate
        {
            Id = ForexPairId.From(fromSymbol, toSymbol),
            FromSymbol = fromSymbol,
            ToSymbol = toSymbol
        };

        aggregate.RaiseEvent(new ForexCreated(fromSymbol, toSymbol, open, high, low, close, fxDate));

        return aggregate;
    }

    public void Update(string fromSymbol, string toSymbol, decimal open, decimal high, decimal low, decimal close, DateTimeOffset fxDate)
    {
        ValidateForexData(fromSymbol, toSymbol, open, high, low, close, fxDate);
        RaiseEvent(new ForexUpdated(fromSymbol, toSymbol, open, high, low, close, fxDate));
    }

    // --- Event Mutator ---

    public void On(ForexCreated evt)
    {
        ValidateForexEventData(evt.FromSymbol, evt.ToSymbol, evt.Open, evt.High, evt.Low, evt.Close, evt.ForexDate);

        Id = evt.AggregateId;
        FromSymbol = evt.FromSymbol;
        ToSymbol = evt.ToSymbol;
        Open = evt.Open;
        High = evt.High;
        Low = evt.Low;
        Close = evt.Close;
        ForexDate = evt.ForexDate;
        OccurredAt = evt.OccurredAt;
    }

    public void On(ForexUpdated evt)
    {
        ValidateForexEventData(evt.FromSymbol, evt.ToSymbol, evt.Open, evt.High, evt.Low, evt.Close, evt.ForexDate);

        FromSymbol = evt.FromSymbol;
        ToSymbol = evt.ToSymbol;
        Open = evt.Open;
        High = evt.High;
        Low = evt.Low;
        Close = evt.Close;
        ForexDate = evt.ForexDate;
        OccurredAt = evt.OccurredAt;
    }

    // --- Validation ---

    private static void ValidateForexData(
        string fromSymbol,
        string toSymbol,
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        DateTimeOffset fxDate)
    {
        if (string.IsNullOrWhiteSpace(fromSymbol))
            throw new InvalidForexException("FromSymbol cannot be empty");

        if (string.IsNullOrWhiteSpace(toSymbol))
            throw new InvalidForexException("ToSymbol cannot be empty");

        if (fromSymbol == toSymbol)
            throw new InvalidForexException("FromSymbol and ToSymbol cannot be the same");

        if (open <= 0)
            throw new InvalidForexException("Open price must be greater than 0");

        if (high <= 0)
            throw new InvalidForexException("High price must be greater than 0");

        if (low <= 0)
            throw new InvalidForexException("Low price must be greater than 0");

        if (close <= 0)
            throw new InvalidForexException("Close price must be greater than 0");

        if (high < low)
            throw new InvalidForexException("High price cannot be less than Low price");

        if (fxDate == default)
            throw new InvalidForexException("ForexDate cannot be empty");
    }

    private static void ValidateForexEventData(
        string fromSymbol,
        string toSymbol,
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        DateTimeOffset fxDate)
    {
        ValidateForexData(fromSymbol, toSymbol, open, high, low, close, fxDate);
    }
}