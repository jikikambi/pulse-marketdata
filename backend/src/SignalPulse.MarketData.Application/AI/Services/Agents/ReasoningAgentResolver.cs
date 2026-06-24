using Microsoft.Extensions.Options;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.AI.Services.Providers;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class ReasoningAgentResolver(IOptions<AiReasoningOptions> options,
     IEnumerable<IReasoningAgent> agents) : IReasoningAgentResolver
{

    private readonly Dictionary<string, IReasoningAgent> _agents = agents.ToDictionary(x => x.Name);

    public IReasoningAgent GetPrimary()
    {
        var name = options.Value.Provider switch
        {
            ReasoningProvider.Template => AgentNames.Template,
            ReasoningProvider.SemanticKernel => AgentNames.SemanticKernel,
            _ => AgentNames.Template
        };

        return _agents[name];
    }

    public IReasoningAgent? GetFallback()
    {
        if (options.Value.Provider == ReasoningProvider.Template)
            return null;

        return _agents[AgentNames.Template];
    }
}