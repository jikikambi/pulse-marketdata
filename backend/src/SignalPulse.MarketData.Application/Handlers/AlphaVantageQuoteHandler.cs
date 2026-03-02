using SignalPulse.MarketData.Application.Common;
using SignalPulse.MarketData.Application.Interfaces;
using SignalPulse.MarketData.Contracts.Events;
using SignalPulse.MarketData.Domain.Common;
using SignalPulse.MarketData.Domain.Quotes;
using SignalPulse.MarketData.Infrastructure.Messaging;
using SignalPulse.MarketData.Infrastructure.Persistence;
using SignalPulse.MarketData.Infrastructure.ReadModels;
using SignalPulse.MarketData.Infrastructure.RedisStore;
using SignalPulse.Rdm.MarketData.AlphaVantage;

namespace SignalPulse.MarketData.Application.Handlers;

/// <summary>
/// Handler orchestrating domain logic, AI insight, read model, and integration event.
/// </summary>
public sealed class AlphaVantageQuoteHandler(IAggregateRepository repo,
IIdempotencyStore idemStore,
IDomainEventPublisher publisher,
IAiInsightProvider ai,
IReadModelRepository<QuoteInsightReadModel> insightsRepo) : AggregateCommandHandler<QuoteAggregate>(repo, idemStore, publisher)
{
    private readonly IAggregateRepository _repo = repo;
    private readonly IDomainEventPublisher _publisher = publisher;
    private readonly IAiInsightProvider _ai = ai;
    private readonly IReadModelRepository<QuoteInsightReadModel> _insightsRepo = insightsRepo;

    public async Task Handle(AlphaVantageQuoteRdm quoteRdm, CancellationToken ct)
    {
        if (!await CheckIdempotencyAsync(quoteRdm.MessageId, ct)) return;
        
        var aggregateId = QuoteId.From(quoteRdm.Symbol);
        var quote = await _repo.LoadAsync<QuoteAggregate>(aggregateId, ct);

        if (quote is null)
        {
            quote = QuoteAggregate.Create(quoteRdm.Symbol, quoteRdm.Price);
        }
        else
        {
            if (quote.Price != quoteRdm.Price || quote.Symbol != quoteRdm.Symbol)
            {
                quote.Update(quoteRdm.Symbol, quoteRdm.Price);
            }
        }

        await PersistAndPublishAsync(quote, ct);

        // --- DOMAIN IS DONE ---
        // Everything from here on is application layer
        
        var insight = await _ai.GenerateAsync(quote.Symbol, quote.Price, quoteRdm.ChangePercent, quoteRdm.Volume, ct);        
        var insightId = Guid.NewGuid();
        var read = new QuoteInsightReadModel
        {
            Id = insightId,
            Symbol = quote.Symbol,
            Price = quote.Price,
            Sentiment = insight.Sentiment,
            Direction = insight.Direction,
            Volatility = insight.Volatility,
            Rationale = insight.Rationale,
            ObservedAt = DateTimeOffset.UtcNow
        };

        await _insightsRepo.UpsertAsync(read, ct);
        await PublishAIInsightEvent(aggregateId, quote, insight, insightId, ct);
    }

    private async Task PublishAIInsightEvent(Guid aggregateId, QuoteAggregate quote, AI.AIInsightResult insight, Guid insightId, CancellationToken ct)
    {
        var evt = new AIInsightGenerated(aggregateId, quote.Symbol, quote.Price, insight.Sentiment, insight.Direction, insight.Volatility, insight.Rationale, DateTimeOffset.UtcNow);

        await _publisher.PublishAsync(eventType: "quote.ai.insight", eventId: insightId, payload: evt, timestamp: DateTimeOffset.UtcNow, ct);
    }
}