using MarketData.Adapter.Api.Client.Services;
using MarketData.Adapter.Shared.AlphaVantage.Request;
using MarketData.Adapter.Shared.AlphaVantage.Response;
using MarketData.Adapter.Shared.Mappers;
using MarketData.Adapter.Shared.Options;
using SignalPulse.AI.SemanticKernel;
using SignalPulse.MarketData.Application.AI.Services.Agents;
using SignalPulse.MarketData.Infrastructure.Persistence;
using SignalPulse.MarketData.Infrastructure.ReadModels;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace MarketData.Adapter.Handler;

[ExcludeFromCodeCoverage(Justification = "No logic")]
public static class ServiceCollectionExtensions
{
    public static void SignalREndpoint(this WebApplication app)
    {
        app.MapGet("/config", (IConfiguration configuration) =>
        {
            return Results.Json(new
            {
                signalRHubUrl = configuration.GetValue<string>("SignalR:BaseUrl")
            });
        });
    }

    public static void MapMinimalApis(this WebApplication app)
    {
        var aiGrp = app.MapGroup("api/agent");

        aiGrp.MapGet("/debug/{key}", async (string key, MarketAgentReplayService replayService) =>
        {
            var replay = await replayService.ReplayAsync(key);

            if (replay is null)
                return Results.NotFound();

            return Results.Ok(replay);
        });

        var spGrp = app.MapGroup("api/signalpulse");

        spGrp.MapGet("/quotes", async (IReadModelRepository<QuoteReadModel> repo, CancellationToken ct) =>
        {
            var quotes = await repo.GetAllAsync(ct);
            return Results.Ok(quotes);
        });

        spGrp.MapGet("/insights", async (IReadModelRepository<QuoteInsightReadModel> repo, CancellationToken ct) =>
        {
            var insights = await repo.GetAllAsync(ct);
            return Results.Ok(insights);
        });

        spGrp.MapGet("/quotes/stream", async (IReadModelRepository<QuoteReadModel> repo, HttpResponse response, CancellationToken ct) =>
        {
            await StreamJsonArray(repo.StreamAllAsync(ct), response, ct);
        });

        spGrp.MapGet("/insights/stream", async (IReadModelRepository<QuoteInsightReadModel> repo, HttpResponse response, CancellationToken ct) =>
        {
            await StreamJsonArray(repo.StreamAllAsync(ct), response, ct);
        });
    }

    public static void AddHandlerServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Options
        services.Configure<AlphaVantageOptions>(configuration.GetSection("AlphaVantage"));
        services.Configure<AlphaVantageSimulationOptions>(configuration.GetSection("AlphaVantageSimulation"));
        services.Configure<ModelSecretsOptions>(configuration.GetSection("ModelSecrets"));
        services.Configure<PollingOptions>(configuration.GetSection("Polling"));
        services.Configure<AgentDebugOptions>(configuration.GetSection("AgentDebug"));

        services.AddSingleton<IAlphaVantageQuoteMapper, AlphaVantageQuoteMapper>();
        services.AddSingleton<IAlphaVantageForexDailyMapper, AlphaVantageForexDailyMapper>();
        services.AddSingleton<IAlphaVantageFallbackService<AlphaVantageQuoteRequest, AlphaVantageQuoteResponse>, AlphaVantageQuoteFallbackService>();
        services.AddSingleton<IAlphaVantageFallbackService<AlphaVantageForexDailyRequest, AlphaVantageForexDailyResponse>, AlphaVantageForexFallbackService>();
    }

    static async Task StreamJsonArray<T>(IAsyncEnumerable<T> source, HttpResponse response, CancellationToken ct)
    {
        response.ContentType = "application/json";

        await using var writer = new Utf8JsonWriter(response.BodyWriter);
        writer.WriteStartArray();

        await foreach (var item in source.WithCancellation(ct))
        {
            JsonSerializer.Serialize(writer, item);
            await writer.FlushAsync(ct);
        }

        writer.WriteEndArray();
        await writer.FlushAsync(ct);
    }
}