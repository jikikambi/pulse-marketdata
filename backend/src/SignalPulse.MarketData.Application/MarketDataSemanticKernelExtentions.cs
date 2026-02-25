using Microsoft.Extensions.DependencyInjection;
using SignalPulse.AI.SemanticKernel;
using SignalPulse.MarketData.Application.Interfaces;
using SignalPulse.MarketData.Application.Services;

namespace SignalPulse.MarketData.Application;

public static class MarketDataSemanticKernelExtentions
{
    public static IServiceCollection AddMarketDataSemanticKernel(this IServiceCollection services)
    {
        services.AddPulseSemanticKernel(builder => 
        {
        });

        services.AddSingleton<QuoteInsightPrompt>();
        services.AddSingleton<IAiMarketInsightGenerator, AiMarketInsightGenerator>();

        return services;
    }
}
