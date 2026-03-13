using SignalPulse.MarketData.Infrastructure.ReadModels;

namespace SignalPulse.MarketData.Infrastructure.Persistence;

public interface IReadModelRepository<T>
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Frontend-safe list.
    /// </summary>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Streaming for internal use
    /// </summary>
    IAsyncEnumerable<T> StreamAllAsync(CancellationToken ct);
    Task UpsertAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

