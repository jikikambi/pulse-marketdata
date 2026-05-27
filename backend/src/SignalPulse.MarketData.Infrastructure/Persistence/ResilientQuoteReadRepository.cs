using Polly;
using SignalPulse.MarketData.Infrastructure.Policies;
using SignalPulse.MarketData.Infrastructure.ReadModels;

namespace SignalPulse.MarketData.Infrastructure.Persistence;

public sealed class ResilientQuoteReadRepository(IReadModelRepository<QuoteReadModel> inner,
    IAiPolicyRegistry policyRegistry)
    : IReadModelRepository<QuoteReadModel>
{
    private readonly IAsyncPolicy _policy =  policyRegistry.GetDataAccessPolicy();

    public Task<QuoteReadModel?> GetByIdAsync(Guid id, CancellationToken ct = default)=> _policy.ExecuteAsync(() => inner.GetByIdAsync(id, ct));
    public Task<IReadOnlyList<QuoteReadModel>> GetAllAsync(CancellationToken ct = default)=> _policy.ExecuteAsync(() => inner.GetAllAsync(ct));
    public IAsyncEnumerable<QuoteReadModel> StreamAllAsync(CancellationToken ct)=> inner.StreamAllAsync(ct);
    public Task UpsertAsync(QuoteReadModel entity, CancellationToken ct = default)=> inner.UpsertAsync(entity, ct);
    public Task DeleteAsync(Guid id, CancellationToken ct = default)=> inner.DeleteAsync(id, ct);
}