using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using SignalPulse.AI.SemanticKernel;
using SignalPulse.MarketData.Application.AI.Cache;
using SignalPulse.MarketData.Application.AI.Caching;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Plugins;
using SignalPulse.MarketData.Application.AI.Services.Agents;
using SignalPulse.MarketData.Application.AI.Services.Memory;
using SignalPulse.MarketData.Application.AI.Services.Prompts;
using SignalPulse.MarketData.Application.AI.Services.Providers;
using SignalPulse.MarketData.Application.Interfaces;
namespace SignalPulse.MarketData.Application;

public static class MarketDataSemanticKernelExtentions
{
    public static IServiceCollection AddMarketDataSemanticKernel(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IQuoteCache, RedisQuoteCache>();
        services.AddSingleton<IAgentStateStore, RedisAgentStateStore>();

        services.AddPulseSemanticKernel(builder =>
        {
            builder.Plugins.AddFromType<QuoteInfoPlugin>();
        });

        bool useMock = configuration.GetValue<bool>("Ai:UseMock");

        services.AddSingleton<QuoteInsightPrompt>();
        services.AddSingleton<ForexInsightPrompt>();
        services.AddSingleton<MarketAgentReplayService>();
        services.AddSingleton<MarketAgentDebugger>();
        services.AddScoped<IQuoteInfoTool, QuoteInfoPlugin>();
        services.AddScoped<IKernelInvoker, SemanticKernelInvoker>();
        services.AddScoped<IRiskAgent, RiskAgent>();
        services.AddScoped<IValidatorAgent, ValidatorAgent>();
        services.AddScoped<IConfidenceScoringAgent, ConfidenceScoringAgent>();
        services.AddScoped<IFinalDecisionAgent, FinalDecisionAgent>();
        services.AddScoped<MarketAgentEngine>();

        if (useMock)
        {
            services.AddSingleton<IAiInsightProvider<QuoteInsightInput>, MockQuoteInsightProvider>();
            services.AddSingleton<IAiInsightProvider<ForexInsightInput>, MockForexInsightProvider>();
        }
        else
        {
            services.AddSingleton<IAiInsightProvider<QuoteInsightInput>, SemanticKernelQuoteInsightProvider>();
            services.AddSingleton<IAiInsightProvider<ForexInsightInput>, SemanticKernelForexInsightProvider>();
            services.AddSingleton<IAiInsightProvider<QuoteInsightInput>, AgentQuoteInsightProvider>();
        }

        return services;
    }
}
