using Microsoft.Extensions.Logging;
using SignalPulse.MarketData.Application.AI;
using SignalPulse.MarketData.Application.Common;
using SignalPulse.MarketData.Application.Interfaces;
using SignalPulse.MarketData.Contracts.Events;
using SignalPulse.MarketData.Domain.Common;
using SignalPulse.MarketData.Domain.Exceptions;
using SignalPulse.MarketData.Domain.Quotes;
using SignalPulse.MarketData.Infrastructure.Messaging;
using SignalPulse.MarketData.Infrastructure.Persistence;
using SignalPulse.MarketData.Infrastructure.ReadModels;
using SignalPulse.MarketData.Infrastructure.RedisStore;
using SignalPulse.Rdm.MarketData.AlphaVantage;

namespace SignalPulse.MarketData.Application.Handlers;

/// <summary>
/// Handler for processing Alpha Vantage quote data with AI insight generation.
/// Inherits orchestration logic from generic base handler, focuses on Quote-specific domain logic.
/// </summary>
public sealed class AlphaVantageQuoteHandler(
    IAggregateRepository repo,
    IIdempotencyStore idemStore,
    IDomainEventPublisher publisher,
    IAiInsightProvider<QuoteInsightInput> ai,
    IReadModelRepository<QuoteInsightReadModel> insightsRepo,
    ILogger<AlphaVantageQuoteHandler> logger)
    : MarketDataInsightHandler<QuoteAggregate, AlphaVantageQuoteRdm, QuoteInsightInput, QuoteInsightReadModel>(
        repo, idemStore, publisher, ai, insightsRepo)
{
    protected override Guid ExtractMessageId(AlphaVantageQuoteRdm rdm) => rdm.MessageId;

    protected override Guid CreateAggregateId(AlphaVantageQuoteRdm rdm) => QuoteId.From(rdm.Symbol);

    protected override QuoteAggregate CreateAggregate(AlphaVantageQuoteRdm rdm)
    {
        try
        {
            return QuoteAggregate.Create(rdm.Symbol, rdm.Price, rdm.ChangePercent);
        }
        catch (InvalidQuoteException ex)
        {
            logger.LogError($"Invalid quote data: {ex.Message}");
            throw;
        }
    }        

    protected override bool ShouldUpdate(QuoteAggregate aggregate, AlphaVantageQuoteRdm rdm) =>
        true;

    protected override void UpdateAggregate(QuoteAggregate aggregate, AlphaVantageQuoteRdm rdm)
    {
        try
        {
            aggregate.Update(rdm.Symbol, rdm.Price, rdm.ChangePercent);
        }
        catch (InvalidQuoteException ex)
        {
            logger.LogError($"Failed to update quote aggregate: {ex.Message}");
            throw;
        }
    }        

    protected override QuoteInsightInput CreateInsightInput(QuoteAggregate aggregate, AlphaVantageQuoteRdm rdm) =>
        new(aggregate.Symbol, aggregate.Price, rdm.ChangePercent, rdm.Volume);

    protected override QuoteInsightReadModel CreateReadModel(
        QuoteAggregate aggregate,
        AlphaVantageQuoteRdm rdm,
        AIInsightResult insight,
        Guid insightId) => new()
    {
        Id = insightId,
        Symbol = aggregate.Symbol,
        Price = aggregate.Price,
        Sentiment = insight.Sentiment,
        Direction = insight.Direction,
        Volatility = insight.Volatility,
        Rationale = insight.Rationale,
        ObservedAt = rdm.ObservedAtUtc
    };

    protected override async Task PublishInsightEvent(
        Guid aggregateId,
        QuoteAggregate aggregate,
        AIInsightResult insight,
        Guid insightId,
        CancellationToken ct)
    {
        var evt = new QuoteAIInsightGenerated(
            aggregateId,
            aggregate.Symbol,
            aggregate.Price,
            insight.Sentiment,
            insight.Direction,
            insight.Volatility,
            insight.Rationale,
            DateTimeOffset.UtcNow);

        await Publisher.PublishAsync(
            eventType: evt.EventType,
            eventId: insightId,
            payload: evt.Payload,
            timestamp: evt.ObservedAt,
            ct);
    }
}