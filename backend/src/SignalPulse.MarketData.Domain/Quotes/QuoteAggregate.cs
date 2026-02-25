using SignalPulse.MarketData.Contracts.Events;
using SignalPulse.MarketData.Domain.Common;
using SignalPulse.MarketData.Domain.Exceptions;

namespace SignalPulse.MarketData.Domain.Quotes;

/// <summary>
/// The price of a symbol at a point in time
/// </summary>
public sealed class QuoteAggregate : AggregateRoot
{
    public string Symbol { get; private set; } = default!;
    public decimal Price { get; private set; }

    public static QuoteAggregate Create(string symbol, decimal price)
    {
        if (price <= 0)
            throw new InvalidQuoteException("Price must be > 0");

        var quote = new QuoteAggregate { Id = QuoteId.From(symbol) };
        quote.RaiseEvent(new QuoteCreated(symbol, price));
        return quote;
    }

    public void Update(string symbol, decimal price) => RaiseEvent(new QuoteUpdated(symbol, price));

    // --- Event Mutator ---
    public void On(QuoteCreated evt)
    {
        Id = evt.AggregateId;
        Symbol = evt.Symbol;
        Price = evt.Price;
    }

    public void On(QuoteUpdated evt)
    {
        Symbol = evt.Symbol;
        Price = evt.Price;
    }
}
