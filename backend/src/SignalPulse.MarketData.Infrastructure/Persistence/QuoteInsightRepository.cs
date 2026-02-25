using Marten;
using SignalPulse.MarketData.Infrastructure.ReadModels;

namespace SignalPulse.MarketData.Infrastructure.Persistence;

public class QuoteInsightRepository(IDocumentSession session) : IReadModelRepository<QuoteInsightReadModel>
{
    public async Task<QuoteInsightReadModel?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await session.Query<QuoteInsightReadModel>().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<QuoteInsightReadModel>> GetAllAsync(CancellationToken ct = default)
        => await session.Query<QuoteInsightReadModel>().ToListAsync(ct);

    public async Task UpsertAsync(QuoteInsightReadModel entity, CancellationToken ct = default)
    {
        session.Store(entity);
        await session.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        session.Delete<QuoteInsightReadModel>(id);
        await session.SaveChangesAsync(ct);
    }
}