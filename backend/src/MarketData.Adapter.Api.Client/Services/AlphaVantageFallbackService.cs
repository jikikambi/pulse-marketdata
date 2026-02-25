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

public sealed class AlphaVantageFallbackService(ILogger<AlphaVantageFallbackService> logger,
     IOptions<AlphaVantageSimulationOptions> simOptions) : IAlphaVantageFallbackService
{

    private readonly Dictionary<string, AlphaVantageQuote> _seededQuotes = DataHelper
        .GetDataAsDeserializedObjectAsync<Dictionary<string, AlphaVantageQuote>>("SeedData/AlphaVantage", "alpha_vantage_seed.json")
        .GetAwaiter().GetResult() ?? [];

    private readonly RefitSettings _refitSettings = new();

    public async Task<ApiResponse<AlphaVantageQuoteResponse>> TryGetOrFallbackAsync(AlphaVantageQuoteRequest request,
        Func<Task<ApiResponse<AlphaVantageQuoteResponse>>> apiCall, CancellationToken ct, bool useLive = true)
    {
        if (!useLive)
        {
            logger.LogDebug("Using DEV MODE — always falling back for {Symbol}", request.Symbol);
            return BuildFallbackResponse(request.Symbol);
        }

        try
        {
            var response = await apiCall();

            if (response.IsSuccessStatusCode && response.Content is { Quote: { Symbol.Length: > 0 } })
                return response;

            logger.LogWarning("AlphaVantage returned invalid data. Falling back for {Symbol}.", request.Symbol);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "AlphaVantage request failed. Falling back for {Symbol}.", request.Symbol);
        }

        return BuildFallbackResponse(request.Symbol);
    }

    private ApiResponse<AlphaVantageQuoteResponse> BuildFallbackResponse(string symbol)
    {
        AlphaVantageQuoteResponse content;

        if (_seededQuotes.TryGetValue(symbol, out var seed))
        {
            // Deep clone
            var quote = new AlphaVantageQuote
            {
                Symbol = seed.Symbol,
                Open = seed.Open,
                High = seed.High,
                Low = seed.Low,
                Price = seed.Price,
                PreviousClose = seed.PreviousClose,
                Change = seed.Change,
                ChangePercent = seed.ChangePercent,
                Volume = seed.Volume,
                LatestTradingDay = seed.LatestTradingDay
            };

            if (simOptions.Value.EnableSimulation)
                ApplySimulation(symbol, quote);

            content = new AlphaVantageQuoteResponse { Quote = quote };
        }
        else
        {
            content = CreateInitialFallback(symbol);

            logger.LogWarning("No seed data exists for {Symbol}. Returning empty fallback.", symbol);
        }

        var msg = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(content))
        };

        return new ApiResponse<AlphaVantageQuoteResponse>(msg, content, settings: _refitSettings);
    }

    private static AlphaVantageQuoteResponse CreateInitialFallback(string symbol) => new()
    {
        Quote = new AlphaVantageQuote
        {
            Symbol = symbol,
            Open = "0",
            High = "0",
            Low = "0",
            Price = "0",
            PreviousClose = "0",
            Change = "0",
            ChangePercent = "0%",
            Volume = "0",
            LatestTradingDay = DateTime.UtcNow.ToString("yyyy-MM-dd")
        }
    };

    // -------------------------------------------------------
    // SIMULATION ENGINE — realistic market movement
    // -------------------------------------------------------
    private void ApplySimulation(string symbol, AlphaVantageQuote quote)
    {
        var rnd = Random.Shared;
        var price = decimal.Parse(quote.Price, CultureInfo.InvariantCulture);
        var drift = 1 + ((decimal)rnd.NextDouble() - 0.5m) * 2 * simOptions.Value.Volatility;
        var newPrice = price + drift;

        // update OHLC
        quote.Open = price.ToString(CultureInfo.InvariantCulture);
        quote.Price = newPrice.ToString("0.00", CultureInfo.InvariantCulture);
        quote.High = Math.Max(decimal.Parse(quote.High), newPrice).ToString(CultureInfo.InvariantCulture);
        quote.Low = Math.Min(decimal.Parse(quote.Low), newPrice).ToString(CultureInfo.InvariantCulture);

        // update volume
        var volume = long.Parse(quote.Volume);
        var volumeJitter = 1 + ((decimal)rnd.NextDouble() - 0.5m) * 2 * simOptions.Value.MaxVolumeJitterPercent;
        quote.Volume = ((long)(volume * volumeJitter)).ToString();

        // compute change
        var prevClose = decimal.Parse(quote.PreviousClose, CultureInfo.InvariantCulture);
        var change = newPrice - prevClose;
        var changePercent = change / prevClose * 100m;

        quote.Change = change.ToString("0.00", CultureInfo.InvariantCulture);
        quote.ChangePercent = $"{changePercent:0.00}%";

        // update timestamp
        quote.LatestTradingDay = DateTime.UtcNow.ToString("yyyy-MM-dd");

        logger.LogDebug("Simulated price for {Symbol}: {Price}", symbol, quote.Price);
    }
}