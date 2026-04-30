using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Services.Memory;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public class MarketAgentDebugger(IAgentStateStore store)
{
    public async Task<AgentDebugView?> GetSessionAsync(string key)
    {
        var state = await store.GetAsync(key);

        if (state is null) return null;

        return new AgentDebugView(
            Key: key,
            CorrelationId: state.CorrelationId,
            Symbol: state.Symbol,
            Plan: state.PlanJson,
            ToolUsed: state.ToolUsed,
            Completed: state.Completed,
            Steps: state.Steps
        );
    }

    public async Task<IEnumerable<AgentDebugView>> GetAllSessionsAsync()
    {
        var keys = await store.GetKeysAsync("*");

        var result = new List<AgentDebugView>();

        foreach (var key in keys)
        {
            var session = await GetSessionAsync(key);
            if (session is not null)
                result.Add(session);
        }

        return result;
    }
}