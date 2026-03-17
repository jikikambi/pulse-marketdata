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

public sealed class QuotePollingWorker(IServiceScopeFactory factory,
    IMarketDataAdapterClient api,
    IOptions<AlphaVantageOptions> provider,
    IOptions<ModelSecretsOptions> secrets,
    IOptions<PollingOptions> polling,
    IAlphaVantageQuoteMapper mapper,
    ILogger<QuotePollingWorker> logger,
    IAlphaVantageFallbackService<AlphaVantageQuoteRequest, AlphaVantageQuoteResponse> fallbackService) : BackgroundService
{
    private readonly AsyncRetryPolicy<ApiResponse<AlphaVantageQuoteResponse>> _retryPolicy = Policy<ApiResponse<AlphaVantageQuoteResponse>>
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
            .GetRequiredService<ValidatedApiClient<AlphaVantageQuoteRequest, ApiResponse<AlphaVantageQuoteResponse>>>();

        foreach (var symbol in provider.Value.QuoteSymbols)
        {
            var request = CreateRequest(symbol);

            ApiResponse<AlphaVantageQuoteResponse> response;

            try
            {
                response = await fallbackService.TryGetOrFallbackAsync(request,
                    () => _retryPolicy.ExecuteAsync(() => validatedClient.Execute(request,
                    (r, ct) => api.GetQuoteAsync(r, ct), stoppingToken)),
                    stoppingToken,
                    useLive: provider.Value.UseLive);
            }
            catch (ValidationException vex)
            {
                logger.LogWarning(vex, "Validation failed for {Symbol}", symbol);
                continue; // skip to next symbol
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "AlphaVantage request failed for {Symbol}", symbol);
                continue;
            }

            var message = mapper.MapTo(response);

            if (message is null)
            {
                logger.LogDebug("Skipping invalid AlphaVantage quote for {Symbol}", symbol);
                continue;
            }
                        
            var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            await publisher.Publish(message, stoppingToken);

            await DelayBetweenRequests(stoppingToken);
        }
    }

    private AlphaVantageQuoteRequest CreateRequest(string symbol)
        => new()
        {
            Function = "GLOBAL_QUOTE",
            Symbol = symbol,
            Apikey = secrets.Value.AlphaVantageApiKey
        };

    private async Task DelayBetweenRequests(CancellationToken ct)
    {
        if (polling.Value.Jitter is not { } jitter)
            return;

        await Task.Delay(Jitter.Next(jitter), ct);
    }
}