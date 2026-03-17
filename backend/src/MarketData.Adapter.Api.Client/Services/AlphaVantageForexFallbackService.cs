using MarketData.Adapter.Shared.AlphaVantage.Request;
using MarketData.Adapter.Shared.AlphaVantage.Response;
using MarketData.Adapter.Shared.Options;
using MarketData.Adapter.Shared.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Refit;
using System.Globalization;
using System.Net;

namespace MarketData.Adapter.Api.Client.Services;

public sealed class AlphaVantageForexFallbackService(ILogger<AlphaVantageForexFallbackService> logger,
        IOptions<AlphaVantageSimulationOptions> options) : IAlphaVantageFallbackService<AlphaVantageForexDailyRequest, AlphaVantageForexDailyResponse>
{

    private readonly Dictionary<string, AlphaVantageForexDailyResponse> _seededFxData = DataHelper.GetDataAsDeserializedObjectAsync<Dictionary<string, AlphaVantageForexDailyResponse>>("SeedData/AlphaVantage", "alpha_vantage_fx.json")
        .GetAwaiter().GetResult() ?? [];

    private readonly RefitSettings _refitSettings = new();

    public async Task<ApiResponse<AlphaVantageForexDailyResponse>> TryGetOrFallbackAsync(AlphaVantageForexDailyRequest request, Func<Task<ApiResponse<AlphaVantageForexDailyResponse>>> apiCall, CancellationToken ct = default, bool useLive = true)
    {
        if (!useLive)
        {
            logger.LogDebug("Using DEV MODE — always falling back for {FromSymbol}/{ToSymbol}", request.FromSymbol, request.ToSymbol);
            return BuildFallbackResponse(request.FromSymbol, request.ToSymbol);
        }

        try
        {
            var response = await apiCall();

            if (response.IsSuccessStatusCode && response.Content?.MetaData != null &&
                !string.IsNullOrWhiteSpace(response.Content.MetaData.FromSymbol) && !string.IsNullOrWhiteSpace(response.Content.MetaData.ToSymbol))
            {
                return response;
            }

            logger.LogWarning( "AlphaVantage returned invalid FX data. Falling back for {FromSymbol}/{ToSymbol}.", request.FromSymbol, request.ToSymbol);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex,"AlphaVantage FX request failed. Falling back for {FromSymbol}/{ToSymbol}.", request.FromSymbol, request.ToSymbol);
        }

        return BuildFallbackResponse(request.FromSymbol, request.ToSymbol);
    }

    private ApiResponse<AlphaVantageForexDailyResponse> BuildFallbackResponse(string fromSymbol, string toSymbol)
    {
        var key = $"{fromSymbol}{toSymbol}";

        AlphaVantageForexDailyResponse content;

        if (_seededFxData.TryGetValue(key, out var seed))
        {
            content = JsonConvert.DeserializeObject<AlphaVantageForexDailyResponse>(JsonConvert.SerializeObject(seed))!;

            if (options.Value.EnableSimulation)
            {
                ApplySimulation(content);
            }
        }
        else
        {
            content = CreateInitialFallback(fromSymbol, toSymbol);
            logger.LogWarning("No seed data exists for {FromSymbol}/{ToSymbol}. Returning empty fallback.", fromSymbol, toSymbol);
        }

        var msg = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(content))
        };

        return new ApiResponse<AlphaVantageForexDailyResponse>(msg, content, settings: _refitSettings);
    }

    private static AlphaVantageForexDailyResponse CreateInitialFallback(string fromSymbol, string toSymbol) => new()
    {
        MetaData = new AlphaVantageForexDaily
        {
            FromSymbol = fromSymbol,
            ToSymbol = toSymbol,
            Information = "Fallback FX Daily Data",
            LastRefreshed = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            OutputSize = "Compact",
            TimeZone = "UTC"
        },
        TimeSeries = new Dictionary<string, AlphaVantageForexDailyRate>
        {
            [DateTime.UtcNow.ToString("yyyy-MM-dd")] = new AlphaVantageForexDailyRate
            {
                Open = "0.00000",
                High = "0.00000",
                Low = "0.00000",
                Close = "0.00000"
            }
        }
    };

    private void ApplySimulation(AlphaVantageForexDailyResponse response)
    {
        var rnd = Random.Shared;
        var volatility = options.Value.Volatility;

        foreach (var kvp in response.TimeSeries)
        {
            var rate = kvp.Value;

            var open = decimal.Parse(rate.Open, CultureInfo.InvariantCulture);
            var high = decimal.Parse(rate.High, CultureInfo.InvariantCulture);
            var low = decimal.Parse(rate.Low, CultureInfo.InvariantCulture);
            var close = decimal.Parse(rate.Close, CultureInfo.InvariantCulture);

            var driftFactor = 1 + ((decimal)rnd.NextDouble() - 0.5m) * 2 * volatility;

            open *= driftFactor;
            high *= driftFactor;
            low *= driftFactor;
            close *= driftFactor;

            // Convert back to string with 5 decimal places (common FX format)
            rate.Open = open.ToString("0.00000", CultureInfo.InvariantCulture);
            rate.High = high.ToString("0.00000", CultureInfo.InvariantCulture);
            rate.Low = low.ToString("0.00000", CultureInfo.InvariantCulture);
            rate.Close = close.ToString("0.00000", CultureInfo.InvariantCulture);
        }

        response.MetaData.LastRefreshed = DateTime.UtcNow.ToString("yyyy-MM-dd");
        logger.LogDebug("Applied simulation to FX fallback data for {FromSymbol}/{ToSymbol}", response.MetaData.FromSymbol, response.MetaData.ToSymbol);
    }
}
