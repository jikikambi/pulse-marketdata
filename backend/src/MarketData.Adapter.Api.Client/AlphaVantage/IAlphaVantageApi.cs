using MarketData.Adapter.Shared.AlphaVantage.Request;
using MarketData.Adapter.Shared.AlphaVantage.Response;
using Refit;

namespace MarketData.Adapter.Api.Client.AlphaVantage;

public interface IAlphaVantageApi
{
    [Get("/query")]
    Task<ApiResponse<AlphaVantageQuoteResponse>> GetQuoteAsync([Query] AlphaVantageQuoteRequest quoteRequest, CancellationToken ct);
}