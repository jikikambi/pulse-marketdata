using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Services.Memory;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public class MarketAgentReplayService(IAgentStateStore store)
{
    public async Task<AgentReplayResult?> ReplayAsync(string key)
    {
        var state = await store.GetAsync(key);

        if (state is null) return null;

        var timeline = new List<string>();

        foreach (var step in state.Steps.OrderBy(s => s.Timestamp).ThenBy(s => s.StepName))
        {
            timeline.Add(
                $"[{step.Timestamp:HH:mm:ss}] {step.StepName}\n" +
                $"INPUT: {step.Input}\n" +
                $"OUTPUT: {step.Output}\n"
            );
        }

        return new AgentReplayResult(
            Key: key,
            Timeline: timeline,
            Completed: state.Completed
        );
    }
}