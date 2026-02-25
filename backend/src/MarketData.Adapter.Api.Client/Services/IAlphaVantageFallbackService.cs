using MarketData.Adapter.Shared.AlphaVantage.Request;
using MarketData.Adapter.Shared.AlphaVantage.Response;
using Refit;

namespace MarketData.Adapter.Api.Client.Services;

public interface IAlphaVantageFallbackService
{
    Task<ApiResponse<AlphaVantageQuoteResponse>> TryGetOrFallbackAsync(AlphaVantageQuoteRequest request, Func<Task<ApiResponse<AlphaVantageQuoteResponse>>> apiCall, CancellationToken ct = default, bool useLive = false);
}