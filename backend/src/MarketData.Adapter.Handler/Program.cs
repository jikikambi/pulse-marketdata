using MarketData.Adapter.Api.Client;
using MarketData.Adapter.Handler;
using MarketData.Adapter.Handler.Handlers;
using MarketData.Adapter.Shared.Middleware;
using SignalPulse.MarketData.Application;
using SignalPulse.MarketData.Infrastructure;
using SignalPulse.MarketData.Infrastructure.Hubs;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureOpenTelemetry();

// --- Options + Mappers ---
builder.Services.AddHandlerServices(builder.Configuration);

// --- Refit ---
builder.Services.AddAlphaVantageApi(builder.Configuration);

// --- CORS ---
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CORS_POLICY, policy =>
    {
        policy.WithOrigins(corsOrigins ?? [])
        .SetIsOriginAllowed(origin => true)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// --- SIGNALR ---
builder.Services.AddSignalR().AddMessagePackProtocol();

// --- Prometheus ---
builder.Services.AddSignalPulsePrometheus();

// --- Redis + Marten  ---
builder.Services.AddMarketDataRedisMarten(builder.Configuration);

// --- Semantic Kernel ---
builder.Services.AddElasticSearch(builder.Configuration);
builder.Services.AddMarketDataSemanticKernel(builder.Configuration);

// --- MassTransit + RabbitMQ + Wolverine ---
builder.Services.AddMarketDataMassTransitRMq(builder.Configuration);
builder.Host.UseWolverine(opts =>
{
    opts.AddMarketDataWolverine(nameof(QuotePollingWorker));
});

builder.Services.AddSignalPulseHealthChecks(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsEnvironment("Docker"))
{
    app.UseHttpsRedirection();
}

// --- CORS ----
app.UseCors(CORS_POLICY);

// --- Observability ---
app.UseSignalPulseObservability();

await app.Services.InitializeElasticAsync();

app.UseWhen(context => context.Request.Path.StartsWithSegments("/api/signalpulse"),
    builder => builder.UseMiddleware<RequestCancellationMiddleware>());

app.MapMinimalApis();

// --- SignalR Hub Endpoint ---
app.MapHub<SignalPulseHub>("/hubs/signalpulse");

// --- /config Dynamic Endpoint for Frontend ---
app.SignalREndpoint();

await app.RunAsync();

public partial class Program
{
    public const string CORS_POLICY = "Frontend";
}