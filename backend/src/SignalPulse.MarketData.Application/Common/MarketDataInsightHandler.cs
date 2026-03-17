using SignalPulse.MarketData.Application.AI;
using SignalPulse.MarketData.Application.Interfaces;
using SignalPulse.MarketData.Domain.Common;
using SignalPulse.MarketData.Infrastructure.Messaging;
using SignalPulse.MarketData.Infrastructure.Persistence;
using SignalPulse.MarketData.Infrastructure.RedisStore;

namespace SignalPulse.MarketData.Application.Common;

/// <summary>
/// Generic base handler that orchestrates domain logic, AI insight generation, read model persistence, and event publishing.
/// </summary>
/// <typeparam name="TAggregate">The domain aggregate (Quote or Forex)</typeparam>
/// <typeparam name="TRdm">The RDM input (AlphaVantageQuoteRdm or AlphaVantageForexRdm)</typeparam>
/// <typeparam name="TInsightInput">The AI insight input (QuoteInsightInput or ForexInsightInput)</typeparam>
/// <typeparam name="TInsightReadModel">The read model for insights</typeparam>
public abstract class MarketDataInsightHandler<TAggregate, TRdm, TInsightInput, TInsightReadModel>(
    IAggregateRepository repo,
    IIdempotencyStore idemStore,
    IDomainEventPublisher publisher,
    IAiInsightProvider<TInsightInput> ai,
    IReadModelRepository<TInsightReadModel> insightsRepo)
    : AggregateCommandHandler<TAggregate>(repo, idemStore, publisher)
    where TAggregate : AggregateRoot, new()
    where TInsightReadModel : class, new()
{
    protected readonly IAiInsightProvider<TInsightInput> Ai = ai;
    protected readonly IReadModelRepository<TInsightReadModel> InsightsRepo = insightsRepo;

    /// <summary>
    /// Main handler orchestration method. Common flow for all market data insight handlers.
    /// </summary>
    public async Task Handle(TRdm rdm, CancellationToken ct)
    {
        if (!await CheckIdempotencyAsync(ExtractMessageId(rdm), ct)) return;

        var aggregateId = CreateAggregateId(rdm);
        var aggregate = await Repository.LoadAsync<TAggregate>(aggregateId, ct);

        if (aggregate is null)
        {
            aggregate = CreateAggregate(rdm);
        }
        else if (ShouldUpdate(aggregate, rdm))
        {
            UpdateAggregate(aggregate, rdm);
        }

        await PersistAndPublishAsync(aggregate, ct);

        // Generate AI insight
        var insightInput = CreateInsightInput(aggregate, rdm);
        var insight = await Ai.GenerateAsync(insightInput, ct);
        var insightId = Guid.NewGuid();

        // Persist read model
        var readModel = CreateReadModel(aggregate, rdm, insight, insightId);
        await InsightsRepo.UpsertAsync(readModel, ct);

        // Publish AI insight event
        await PublishInsightEvent(aggregateId, aggregate, insight, insightId, ct);
    }

    /// <summary>
    /// Extract message ID from RDM for idempotency checking.
    /// </summary>
    protected abstract Guid ExtractMessageId(TRdm rdm);

    /// <summary>
    /// Create the aggregate ID from RDM data.
    /// </summary>
    protected abstract Guid CreateAggregateId(TRdm rdm);

    /// <summary>
    /// Create a new aggregate from RDM data.
    /// </summary>
    protected abstract TAggregate CreateAggregate(TRdm rdm);

    /// <summary>
    /// Determine if aggregate should be updated based on new RDM data.
    /// </summary>
    protected abstract bool ShouldUpdate(TAggregate aggregate, TRdm rdm);

    /// <summary>
    /// Update the aggregate with new RDM data.
    /// </summary>
    protected abstract void UpdateAggregate(TAggregate aggregate, TRdm rdm);

    /// <summary>
    /// Create AI insight input from aggregate and RDM data.
    /// </summary>
    protected abstract TInsightInput CreateInsightInput(TAggregate aggregate, TRdm rdm);

    /// <summary>
    /// Create read model from aggregate, RDM, and AI insight.
    /// </summary>
    protected abstract TInsightReadModel CreateReadModel(TAggregate aggregate, TRdm rdm, AIInsightResult insight, Guid insightId);

    /// <summary>
    /// Publish the AI insight event.
    /// </summary>
    protected abstract Task PublishInsightEvent(Guid aggregateId, TAggregate aggregate, AIInsightResult insight, Guid insightId, CancellationToken ct);
}