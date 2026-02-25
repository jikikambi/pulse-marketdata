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
            quote.Id = evt.QuoteId;
            quote.Symbol = evt.Symbol;
            quote.Price = evt.Price;
            quote.Sentiment = evt.Sentiment;
            quote.Direction = evt.Direction;
            quote.Rationale = evt.Rationale;
            quote.Volatility = evt.Volatility;
            quote.ObservedAt = DateTimeOffset.UtcNow;
        });
    }
}