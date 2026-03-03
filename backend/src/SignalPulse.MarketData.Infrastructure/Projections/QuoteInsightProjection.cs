using Marten.Events.Aggregation;
using SignalPulse.MarketData.Contracts.Events;
using SignalPulse.MarketData.Infrastructure.ReadModels;

namespace SignalPulse.MarketData.Infrastructure.Projections;

public class QuoteInsightProjection : SingleStreamProjection<QuoteInsightReadModel, Guid>
{
    public QuoteInsightProjection()
    {
        ProjectEvent<AIInsightGenerated>((quote, evt) =>
        {
            quote.Id = evt.Id;
            quote.Symbol = evt.Symbol;
            quote.Price = evt.Price;
            quote.Sentiment = evt.Sentiment;
            quote.Direction = evt.Direction;
            quote.Volatility = evt.Volatility;
            quote.Rationale = evt.Rationale;            
            quote.ObservedAt = DateTimeOffset.UtcNow;
        });
    }
}