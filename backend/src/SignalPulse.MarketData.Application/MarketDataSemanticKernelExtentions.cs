using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SignalPulse.AI.SemanticKernel;
using SignalPulse.MarketData.Application.Interfaces;
using SignalPulse.MarketData.Application.Services;
using static SignalPulse.MarketData.Application.Services.SemanticKernelInsightProvider;

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

        if (useMock)
        {
            services.AddSingleton<IAiInsightProvider, MockInsightProvider>();
        }
        else
        {
            services.AddSingleton<IAiInsightProvider, SemanticKernelInsightProvider>();
        }

        return services;
    }
}
  