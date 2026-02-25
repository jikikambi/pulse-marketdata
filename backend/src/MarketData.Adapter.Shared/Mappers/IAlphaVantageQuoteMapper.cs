using MarketData.Adapter.Shared.AlphaVantage.Response;
using Refit;
using SignalPulse.Rdm.MarketData.AlphaVantage;

namespace MarketData.Adapter.Shared.Mappers;

public interface IAlphaVantageQuoteMapper
{
    AlphaVantageQuoteRdm? MapTo(ApiResponse<AlphaVantageQuoteResponse> response);
}