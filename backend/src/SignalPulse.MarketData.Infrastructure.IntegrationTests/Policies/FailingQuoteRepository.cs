using Polly.Timeout;
using SignalPulse.MarketData.Infrastructure.Persistence;
using SignalPulse.MarketData.Infrastructure.ReadModels;

namespace SignalPulse.MarketData.Infrastructure.IntegrationTests.Policies;

public sealed class FailingQuoteRepository(int failuresBeforeSuccess) : IReadModelRepository<QuoteReadModel>
{
    private int _remainingFailures = failuresBeforeSuccess;

    public Task<QuoteReadModel?> GetByIdAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();

    public async Task<IReadOnlyList<QuoteReadModel>> GetAllAsync(CancellationToken ct = default)
    {
        if (_remainingFailures > 0)
        {
            _remainingFailures--;

            throw new HttpRequestException("Injected Marten transient failure");
        }

        return
        [
            new QuoteReadModel
            {
                Id = Guid.NewGuid()
            }
        ];
    }

    public async IAsyncEnumerable<QuoteReadModel> StreamAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        yield return new QuoteReadModel
        {
            Id = Guid.NewGuid()
        };

        await Task.CompletedTask;
    }

    public Task UpsertAsync(QuoteReadModel entity, CancellationToken ct = default) => throw new NotImplementedException();
    public Task DeleteAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
}