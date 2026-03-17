using FluentValidation;
using MarketData.Adapter.Api.Client;
using MarketData.Adapter.Api.Client.Services;
using MarketData.Adapter.Shared.AlphaVantage.Request;
using MarketData.Adapter.Shared.AlphaVantage.Response;
using MarketData.Adapter.Shared.AlphaVantage.Services;
using MarketData.Adapter.Shared.Mappers;
using MarketData.Adapter.Shared.Options;
using MassTransit;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Refit;
using SignalPulse.AI.SemanticKernel;

namespace MarketData.Adapter.Handler.Handlers;

public class ForexPollingWorker(IServiceScopeFactory factory,
    IMarketDataAdapterClient api,
    IOptions<AlphaVantageOptions> provider,
    IOptions<ModelSecretsOptions> secrets,
    IOptions<PollingOptions> polling,
    IAlphaVantageForexDailyMapper mapper,
    ILogger<ForexPollingWorker> logger,
    IAlphaVantageFallbackService<AlphaVantageForexDailyRequest, AlphaVantageForexDailyResponse> fallbackService) : BackgroundService
{

    private readonly AsyncRetryPolicy<ApiResponse<AlphaVantageForexDailyResponse>> _retryPolicy = Policy<ApiResponse<AlphaVantageForexDailyResponse>>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(retryCount: 3, sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(polling.Value.Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PollOnce(stoppingToken);
        }
    }

    internal async Task PollOnce(CancellationToken stoppingToken)
    {
        using var scope = factory.CreateScope();

        var validatedClient = scope.ServiceProvider
            .GetRequiredService<ValidatedApiClient<AlphaVantageForexDailyRequest, ApiResponse<AlphaVantageForexDailyResponse>>>();

        foreach (var (fromSymbol, toSymbol) in provider.Value.ForexSymbols)
        {
            var request = CreateRequest(fromSymbol, toSymbol);

            ApiResponse<AlphaVantageForexDailyResponse> response;

            try
            {
                response = await fallbackService.TryGetOrFallbackAsync(request,
                    () => _retryPolicy.ExecuteAsync(() => validatedClient.Execute(request,
                    (r, ct) => api.GetFxDailyAsync(r, ct), stoppingToken)),
                    stoppingToken,
                    useLive: provider.Value.UseLive);
            }
            catch (ValidationException vex)
            {
                logger.LogWarning(vex, "Validation failed for {From}-{To}", fromSymbol, toSymbol);
                continue;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "AlphaVantage request failed for {From}-{To}", fromSymbol, toSymbol);
                continue;
            }

            var message = mapper.MapTo(response);

            if (message is null)
            {
                logger.LogDebug("Skipping invalid AlphaVantage FX data for {From}-{To}", fromSymbol, toSymbol);
                continue;
            }

            var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            await publisher.Publish(message, stoppingToken);

            await DelayBetweenRequests(stoppingToken);
        }
    }

    private AlphaVantageForexDailyRequest CreateRequest(string fromSymbol, string toSymbol)
        => new()
        {
            Function = "FX_DAILY",
            FromSymbol = fromSymbol,
            ToSymbol = toSymbol,
            Apikey = secrets.Value.AlphaVantageApiKey,
        };

    private async Task DelayBetweenRequests(CancellationToken ct)
    {
        if (polling.Value.Jitter is not { } jitter)
            return;

        await Task.Delay(Jitter.Next(jitter), ct);
    }
}
