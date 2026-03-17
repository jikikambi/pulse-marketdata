using MarketData.Adapter.Shared.AlphaVantage.Request;
using MarketData.Adapter.Shared.AlphaVantage.Response;
using Refit;

namespace MarketData.Adapter.Api.Client.Services;

public interface IAlphaVantageFallbackService<T, R>
{
    Task<ApiResponse<R>> TryGetOrFallbackAsync(T request, Func<Task<ApiResponse<R>>> apiCall, CancellationToken ct = default, bool useLive = false);
}