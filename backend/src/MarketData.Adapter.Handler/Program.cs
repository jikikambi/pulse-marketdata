using MarketData.Adapter.Api.Client;
using MarketData.Adapter.Handler;
using MarketData.Adapter.Handler.Handlers;
using SignalPulse.MarketData.Application;
using SignalPulse.MarketData.Infrastructure;
using SignalPulse.MarketData.Infrastructure.Hubs;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

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

// --- Redis + Marten  ---
builder.Services.AddMarketDataRedisMarten(builder.Configuration);

// --- Semantic Kernel ---
builder.Services.AddMarketDataSemanticKernel(builder.Configuration);

// --- MassTransit + RabbitMQ + Wolverine ---
builder.Services.AddMarketDataMassTransitRMq(builder.Configuration);
builder.Host.UseWolverine(opts =>
{
    opts.AddMarketDataWolverine(nameof(QuotePollingWorker));   
});

// --- Background Worker ---
builder.Services.AddHostedService<QuotePollingWorker>();

var app = builder.Build();

app.UseHttpsRedirection();

// --- CORS ----
app.UseCors(CORS_POLICY);

// --- SignalR Hub Endpoint ---
app.MapHub<SignalPulseHub>("/hubs/signalpulse");

// --- /config Dynamic Endpoint for Frontend ---
app.SignalREndpoint();

await app.RunAsync();

public partial class Program
{
    public const string CORS_POLICY = "Frontend";
}