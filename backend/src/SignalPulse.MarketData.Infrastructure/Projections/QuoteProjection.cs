using Marten.Events.Aggregation;
using SignalPulse.MarketData.Contracts.Events;
using SignalPulse.MarketData.Infrastructure.ReadModels;

namespace SignalPulse.MarketData.Infrastructure.Projections;

public class QuoteProjection : SingleStreamProjection<QuoteReadModel, Guid>
{
    public QuoteProjection()
    {
        ProjectEvent<QuoteCreated>((quote, evt) =>
        {
            quote.Id = evt.AggregateId;
            quote.Symbol = evt.Symbol;
            quote.Price = evt.Price;
        });
        ProjectEvent<QuoteUpdated>((quote, evt) =>
        {
            quote.Symbol = evt.Symbol;
            quote.Price = evt.Price;
        });
    }
}