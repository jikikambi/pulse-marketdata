using SignalPulse.MarketData.Contracts.Events;
using SignalPulse.MarketData.Domain.Common;
using SignalPulse.MarketData.Domain.Exceptions;

namespace SignalPulse.MarketData.Domain.Quotes;

public sealed class QuoteAggregate : AggregateRoot
{
    public string Symbol { get; private set; }
    public decimal Price { get; private set; }
    public decimal ChangePercent { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>
    /// Parameterless constructor for ORM/reflection-based instantiation (e.g., event sourcing rehydration).
    /// Direct instantiation is discouraged; use Create() factory method instead.
    /// </summary>
    public QuoteAggregate()
    {
        Symbol = string.Empty;
    }

    public static QuoteAggregate Create(string symbol, decimal price, decimal changePercent)
    {
        ValidateQuoteData(symbol, price);

        var quote = new QuoteAggregate 
        { 
            Id = QuoteId.From(symbol),
            Symbol = symbol
        };
        quote.RaiseEvent(new QuoteCreated(symbol, price, changePercent));
        return quote;
    }

    public void Update(string symbol, decimal price, decimal changePercent)
    {
        ValidateQuoteData(symbol, price);
        RaiseEvent(new QuoteUpdated(symbol, price, changePercent));
    }

    // --- Event Mutator ---
    public void On(QuoteCreated evt)
    {
        ValidateQuoteEventData(evt.Symbol, evt.Price);

        Id = evt.AggregateId;
        Symbol = evt.Symbol;
        Price = evt.Price;
        ChangePercent = evt.ChangePercent;
        OccurredAt = evt.OccurredAt;
    }

    public void On(QuoteUpdated evt)
    {
        ValidateQuoteEventData(evt.Symbol, evt.Price);

        Symbol = evt.Symbol;
        Price = evt.Price;
        ChangePercent = evt.ChangePercent;
        OccurredAt = evt.OccurredAt;
    }

    // --- Validation ---
    
    private static void ValidateQuoteData(string symbol, decimal price)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new InvalidQuoteException("Symbol cannot be empty");

        if (price <= 0)
            throw new InvalidQuoteException("Price must be greater than 0");
    }
    
    private static void ValidateQuoteEventData(string symbol, decimal price)
    {
        ValidateQuoteData(symbol, price);
    }
}