using Microsoft.Extensions.DependencyInjection;
using SignalPulse.MarketData.Application.Handlers;
using Wolverine;
namespace SignalPulse.MarketData.Application;

public static class MarketDataWolverineRmqExtensions
{
    public static void AddMarketDataWolverine(this WolverineOptions opts, string serviceName)
    {
        opts.ServiceName = serviceName;
        opts.Discovery.IncludeAssembly(typeof(AlphaVantageQuoteHandler).Assembly);
        opts.Policies.AutoApplyTransactions();
        opts.Services.AddScoped<AlphaVantageQuoteHandler>();
    }
}