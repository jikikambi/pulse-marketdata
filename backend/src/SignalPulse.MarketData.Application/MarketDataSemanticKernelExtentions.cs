using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SignalPulse.AI.SemanticKernel;
using SignalPulse.MarketData.Application.AI;
using SignalPulse.MarketData.Application.Interfaces;
using SignalPulse.MarketData.Application.Services;
namespace SignalPulse.MarketData.Application;

public static class MarketDataSemanticKernelExtentions
{
    public static IServiceCollection AddMarketDataSemanticKernel(this IServiceCollection services, IConfiguration configuration)
    {

        services.AddPulseSemanticKernel(builder => 
        {
            // Configure kernel as needed
        });

        bool useMock = configuration.GetValue<bool>("Ai:UseMock");

        services.AddSingleton<QuoteInsightPrompt>();
        services.AddSingleton<ForexInsightPrompt>();

        if (useMock)
        {
            services.AddSingleton<IAiInsightProvider<QuoteInsightInput>, MockQuoteInsightProvider>();
            services.AddSingleton<IAiInsightProvider<ForexInsightInput>, MockForexInsightProvider>();
        }
        else
        {
            services.AddSingleton<IAiInsightProvider<QuoteInsightInput>, SemanticKernelQuoteInsightProvider>();
            services.AddSingleton<IAiInsightProvider<ForexInsightInput>, SemanticKernelForexInsightProvider>();
        }

        return services;
    }
}
  