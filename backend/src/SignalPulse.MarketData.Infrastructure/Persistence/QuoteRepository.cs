using Marten;
using SignalPulse.MarketData.Infrastructure.ReadModels;

namespace SignalPulse.MarketData.Infrastructure.Persistence;

public class QuoteRepository(IDocumentSession session) : IReadModelRepository<QuoteReadModel>
{
    public async Task<QuoteReadModel?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await session.Query<QuoteReadModel>().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<QuoteReadModel>> GetAllAsync(CancellationToken ct = default)
        => await session.Query<QuoteReadModel>().ToListAsync(ct);

    public async Task UpsertAsync(QuoteReadModel entity, CancellationToken ct = default)
    {
        session.Store(entity);
        await session.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        session.Delete<QuoteReadModel>(id);
        await session.SaveChangesAsync(ct);
    }
}
