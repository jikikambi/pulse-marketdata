using MarketData.Adapter.Shared.AlphaVantage.Response;
using Refit;
using SignalPulse.Rdm.MarketData.AlphaVantage;

namespace MarketData.Adapter.Shared.Mappers;

public interface IAlphaVantageForexDailyMapper
{
    IEnumerable<AlphaVantageForexRdm>? MapTo(ApiResponse<AlphaVantageForexDailyResponse> response);
}