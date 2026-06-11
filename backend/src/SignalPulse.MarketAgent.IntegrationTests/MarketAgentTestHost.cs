using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.AI.Services.Agents;

namespace SignalPulse.MarketAgent.IntegrationTests;

public sealed class MarketAgentTestHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    public HttpClient Client { get; private set; } = default!;

    public MarketAgentEngine Engine { get; }

    private MarketAgentTestHost(WebApplication app, HttpClient client, MarketAgentEngine engine)
    {
        _app = app;
        Client = client;
        Engine = engine;
    }

    public static async Task<MarketAgentTestHost> CreateAsync(IEnumerable<IMarketAgentStage> stages, RecoveryStrategy recoveryStrategy = RecoveryStrategy.Skip)
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.UseUrls("http://localhost:5025");

        builder.Services.AddOpenTelemetry().WithMetrics(metrics =>
        {
            metrics.AddMeter("SignalPulse.MarketAgent").AddPrometheusExporter();
        });

        var engine = MarketAgentEngineFactory.CreateEngine(stages, recoveryStrategy);

        builder.Services.AddSingleton(engine);

        var app = builder.Build();

        app.MapPrometheusScrapingEndpoint();

        await app.StartAsync();

        return new MarketAgentTestHost(app, new HttpClient { BaseAddress = new Uri("http://localhost:5025") }, engine);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}