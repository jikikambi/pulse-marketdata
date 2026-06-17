using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using SignalPulse.AI.SemanticKernel;
using SignalPulse.MarketData.Application.AI.Cache;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.AI.Plugins;
using SignalPulse.MarketData.Application.AI.Services.Agents;
using SignalPulse.MarketData.Application.AI.Services.Memory;
using SignalPulse.MarketData.Application.AI.Services.Providers;
using SignalPulse.MarketData.Application.AI.Skills.Services;
using SignalPulse.MarketData.Application.Interfaces;
using SignalPulse.MarketData.Infrastructure.Policies;
using SignalPulse.Messaging.RabbitMq;

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

        services.Configure<AiReasoningOptions>(configuration.GetSection("AI:Reasoning"));

        var aiOptions = configuration.GetSection("AI:Reasoning").Get<AiReasoningOptions>() ?? throw new InvalidOperationException("AI Reasoning options missing");

        services.AddScoped<ForexInsightService>();
        services.AddSingleton<MarketAgentReplayService>();
        services.AddSingleton<MarketAgentDebugger>();
        services.AddSingleton<MockQuoteInsightProvider>();
        services.AddScoped<MockQuoteInsightProvider>();
        services.AddScoped<IReasoningAgent, SemanticKernelReasoningAgent>();
        services.AddScoped<IReasoningAgent, TemplateReasoningAgent>();
        services.AddScoped<IReasoningAgentResolver, ReasoningAgentResolver>();
        services.AddSingleton<OllamaReasoningAgent>();
        services.AddScoped<IQuoteInfoTool, QuoteInfoPlugin>();

        //if (aiOptions.Provider == ReasoningProvider.SemanticKernel)
        //{
            services.AddScoped<ISkillRegistry, SemanticKernelSkillRegistry>();
            services.AddScoped<IKernelInvoker, SemanticKernelInvoker>();
        //}

        services.AddSingleton<IAiPolicyRegistry, AiPolicyRegistry>();
        services.AddScoped<IMarketStageOrchestrator, MarketStageOrchestrator>();
        services.AddScoped<IMarketStageScheduler, MarketStageScheduler>();

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

        services.AddScoped<IAiInsightProvider<QuoteInsightInput>, AgentQuoteInsightProvider>();

        if (useMock)
        {
            services.AddSingleton<IAiInsightProvider<ForexInsightInput>, MockForexInsightProvider>();
        }
        else
        {
            services.AddSingleton<IAiInsightProvider<ForexInsightInput>, SemanticKernelForexInsightProvider>();
        }

        return services;
    }
}
