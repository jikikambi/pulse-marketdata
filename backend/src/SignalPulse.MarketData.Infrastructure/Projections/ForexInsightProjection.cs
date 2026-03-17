using Marten.Events.Aggregation;
using SignalPulse.MarketData.Contracts.Events;
using SignalPulse.MarketData.Infrastructure.ReadModels;

namespace SignalPulse.MarketData.Infrastructure.Projections;

public class ForexInsightProjection : SingleStreamProjection<ForexInsightReadModel, Guid>
{
    public ForexInsightProjection()
    {
        ProjectEvent<ForexAIInsightGenerated>((forex, evt) =>
        {
            forex.Id = evt.Id;
            forex.FromSymbol = evt.FromSymbol;
            forex.ToSymbol = evt.ToSymbol;
            forex.Open = evt.Open;
            forex.Close = evt.Close;
            forex.Low = evt.Low;
            forex.High = evt.High;
            forex.ForexDate = evt.ForexDate;
            forex.Sentiment = evt.Sentiment;
            forex.Direction = evt.Direction;
            forex.Volatility = evt.Volatility;
            forex.Rationale = evt.Rationale;
            forex.ObservedAt = DateTimeOffset.UtcNow;
        });
    }
}