using Microsoft.Extensions.Logging;
using SignalPulse.MarketData.Application.AI;
using SignalPulse.MarketData.Application.Common;
using SignalPulse.MarketData.Application.Interfaces;
using SignalPulse.MarketData.Contracts.Events;
using SignalPulse.MarketData.Domain.Common;
using SignalPulse.MarketData.Domain.Exceptions;
using SignalPulse.MarketData.Domain.Forex;
using SignalPulse.MarketData.Infrastructure.Messaging;
using SignalPulse.MarketData.Infrastructure.Persistence;
using SignalPulse.MarketData.Infrastructure.ReadModels;
using SignalPulse.MarketData.Infrastructure.RedisStore;
using SignalPulse.Rdm.MarketData.AlphaVantage;

namespace SignalPulse.MarketData.Application.Handlers;

/// <summary>
/// Handler for processing Alpha Vantage forex data with AI insight generation.
/// Inherits orchestration logic from generic base handler, focuses on Forex-specific domain logic.
/// </summary>
public sealed class AlphaVantageForexHandler(
    IAggregateRepository repo,
    IIdempotencyStore idemStore,
    IDomainEventPublisher publisher,
    IAiInsightProvider<ForexInsightInput> ai,
    IReadModelRepository<ForexInsightReadModel> insightsRepo,
    ILogger<AlphaVantageForexHandler> logger)
    : MarketDataInsightHandler<ForexAggregate, AlphaVantageForexRdm, ForexInsightInput, ForexInsightReadModel>(
        repo, idemStore, publisher, ai, insightsRepo)
{
    protected override Guid ExtractMessageId(AlphaVantageForexRdm rdm) => rdm.MessageId;

    protected override Guid CreateAggregateId(AlphaVantageForexRdm rdm) =>
        ForexPairId.From(rdm.FromSymbol, rdm.ToSymbol);

    protected override ForexAggregate CreateAggregate(AlphaVantageForexRdm rdm)
    {
        try
        {
           return ForexAggregate.Create(rdm.FromSymbol, rdm.ToSymbol, rdm.Open, rdm.High, rdm.Low, rdm.Close, rdm.ForexDate);
        }
        catch(InvalidForexException ex)
        {
            logger.LogError($"Invalid forex data: {ex.Message}");
            throw;
        }
    }        

    protected override bool ShouldUpdate(ForexAggregate aggregate, AlphaVantageForexRdm rdm) =>
        true;

    protected override void UpdateAggregate(ForexAggregate aggregate, AlphaVantageForexRdm rdm)
    {
        try
        {
            aggregate.Update(rdm.FromSymbol, rdm.ToSymbol, rdm.Open, rdm.High, rdm.Low, rdm.Close, rdm.ForexDate);
        }
        catch (InvalidForexException ex)
        {
            logger.LogError($"Invalid forex data: {ex.Message}");
        }
    }

    protected override ForexInsightInput CreateInsightInput(ForexAggregate aggregate, AlphaVantageForexRdm rdm) =>
        new(aggregate.FromSymbol, aggregate.ToSymbol, aggregate.Open, aggregate.High, aggregate.Low, aggregate.Close, aggregate.ForexDate);

    protected override ForexInsightReadModel CreateReadModel(
        ForexAggregate aggregate,
        AlphaVantageForexRdm rdm,
        AIInsightResult insight,
        Guid insightId) => new()
        {
            Id = insightId,
            FromSymbol = aggregate.FromSymbol,
            ToSymbol = aggregate.ToSymbol,
            Open = aggregate.Open,
            Close = aggregate.Close,
            High = aggregate.High,
            Low = aggregate.Low,
            ForexDate = aggregate.ForexDate,
            Sentiment = insight.Sentiment,
            Direction = insight.Direction,
            Volatility = insight.Volatility,
            Rationale = insight.Rationale,
            ObservedAt = rdm.ForexDate
        };

    protected override async Task PublishInsightEvent(
        Guid aggregateId,
        ForexAggregate aggregate,
        AIInsightResult insight,
        Guid insightId,
        CancellationToken ct)
    {
        var evt = new ForexAIInsightGenerated(
            aggregateId,
            aggregate.FromSymbol,
            aggregate.ToSymbol,
            aggregate.Open,
            aggregate.High,
            aggregate.Low,
            aggregate.Close,
            aggregate.ForexDate,
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