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

// --- SIGNALR ---
builder.Services.AddSignalR().AddMessagePackProtocol();

// --- Redis + Marten  ---
builder.Services.AddMarketDataRedisMarten(builder.Configuration);

// --- Semantic Kernel ---
builder.Services.AddMarketDataSemanticKernel();

// --- MassTransit + RabbitMQ + Wolverine ---
builder.Services.AddMarketDataMassTransitRMq(builder.Configuration);
builder.Host.UseWolverine(opts =>
{
    opts.AddMarketDataWolverine(nameof(QuotePollingWorker));   
});

// --- Background Worker ---
builder.Services.AddHostedService<QuotePollingWorker>();

var host = builder.Build();

host.MapHub<SignalPulseHub>("/hubs/signalpulse");

await host.RunAsync();
