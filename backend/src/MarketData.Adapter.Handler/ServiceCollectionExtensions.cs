using MarketData.Adapter.Api.Client.Services;
using MarketData.Adapter.Shared.Mappers;
using MarketData.Adapter.Shared.Options;
using SignalPulse.AI.SemanticKernel;
using System.Diagnostics.CodeAnalysis;

namespace MarketData.Adapter.Handler;

[ExcludeFromCodeCoverage(Justification = "No logic")]
public static class ServiceCollectionExtensions
{
    public static void SignalREndpoint(this WebApplication app)
    {
        app.MapGet("/config", (IConfiguration configuration) => new
        {
            signalRHubUrl = configuration.GetValue<string>("SignalR:BaseUrl")
        });
    }

    public static void AddHandlerServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Options
        services.Configure<AlphaVantageOptions>(configuration.GetSection("AlphaVantage"));
        services.Configure<AlphaVantageSimulationOptions>(configuration.GetSection("AlphaVantageSimulation"));
        services.Configure<ModelSecretsOptions>(configuration.GetSection("ModelSecrets"));
        services.Configure<PollingOptions>(configuration.GetSection("Polling"));

        services.AddSingleton<IAlphaVantageQuoteMapper, AlphaVantageQuoteMapper>();
        services.AddSingleton<IAlphaVantageFallbackService, AlphaVantageFallbackService>();
    }
}