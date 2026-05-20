using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Polly;
using SignalPulse.AI.SemanticKernel;
using SignalPulse.MarketData.Application.AI.Cache;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Plugins;
using SignalPulse.MarketData.Application.AI.Policies;
using SignalPulse.MarketData.Application.AI.Services.Agents;
using SignalPulse.MarketData.Application.AI.Services.Memory;
using SignalPulse.MarketData.Application.AI.Services.Providers;
using SignalPulse.MarketData.Application.AI.Skills.Services;
using SignalPulse.MarketData.Application.Interfaces;
using SignalPulse.MarketData.Infrastructure.Elastic;
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

        services.AddSingleton<QuoteInsightService>();
        services.AddSingleton<ForexInsightService>();
        services.AddSingleton<MarketAgentReplayService>();
        services.AddSingleton<MarketAgentDebugger>();
        services.AddScoped<IQuoteInfoTool, QuoteInfoPlugin>();
        services.AddScoped<ISkillRegistry, SemanticKernelSkillRegistry>();
        services.AddScoped<IKernelInvoker, SemanticKernelInvoker>();

        services.AddSingleton<IAsyncPolicy<string>>(_ => AiRetryPolicies.Create());

        services.AddScoped<IRiskAgent, RiskAgent>();
        services.AddScoped<IValidatorAgent, ValidatorAgent>();
        services.AddScoped<IConfidenceScoringAgent, ConfidenceScoringAgent>();
        services.AddScoped<IFinalDecisionAgent, FinalDecisionAgent>();        
        services.AddScoped<IWorkflowOutcomeFactory, WorkflowOutcomeFactory>();
        services.AddScoped<IMarketAgentStage, ValidationInputStage>();
        services.AddScoped<IMarketAgentStage, PlannerStage>();
        services.AddScoped<IMarketAgentStage, PlanParsingStage>();
        services.AddScoped<IMarketAgentStage, ToolStage>();
        services.AddScoped<IMarketAgentStage, ReasoningStage>();
        services.AddScoped<IMarketAgentStage, ValidationStage>();
        services.AddScoped<IMarketAgentStage, RiskStage>();
        services.AddScoped<IMarketAgentStage, ConfidenceStage>();
        services.AddScoped<IMarketAgentStage, DecisionStage>();
        services.AddScoped<IMarketAgentStage, PersistenceStage>();

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
