using Microsoft.Extensions.Options;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.AI.Services.Providers;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class PlannerAgentResolver(IEnumerable<IPlannerExecutionAgent> agents,
    IOptions<AiReasoningOptions> options) : IPlannerAgentResolver
{
    private readonly Dictionary<string, IPlannerExecutionAgent> _agents = agents.ToDictionary(x => x.Name);

    public IPlannerExecutionAgent GetPrimary()
    {
        return options.Value.Provider switch
        {
            ReasoningProvider.Template => _agents[AgentNames.Template],
            ReasoningProvider.SemanticKernel => _agents[AgentNames.SemanticKernel],
            _ => _agents[AgentNames.Template]
        };
    }

    public IPlannerExecutionAgent? GetFallback()
    {
        if (!options.Value.EnableFallback) return null;

        return options.Value.Provider switch
        {
            ReasoningProvider.SemanticKernel => _agents[AgentNames.Template],
            _ => null
        };
    }
}