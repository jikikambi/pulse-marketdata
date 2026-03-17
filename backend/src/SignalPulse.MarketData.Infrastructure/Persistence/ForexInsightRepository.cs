using Marten;
using SignalPulse.MarketData.Infrastructure.ReadModels;

namespace SignalPulse.MarketData.Infrastructure.Persistence;

public class ForexInsightRepository(IDocumentSession session) : IReadModelRepository<ForexInsightReadModel>
{
    public async Task<ForexInsightReadModel?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await session.Query<ForexInsightReadModel>().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<ForexInsightReadModel>> GetAllAsync(CancellationToken ct = default)
        => await StreamAllAsync(ct).ToListAsync(ct);

    public IAsyncEnumerable<ForexInsightReadModel> StreamAllAsync(CancellationToken ct)
        => session.Query<ForexInsightReadModel>().ToAsyncEnumerable(ct);

    public async Task UpsertAsync(ForexInsightReadModel entity, CancellationToken ct = default)
    {
        session.Store(entity);
        await session.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        session.Delete<ForexInsightReadModel>(id);
        await session.SaveChangesAsync(ct);
    }
}