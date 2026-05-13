using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Services.Memory;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public class MarketAgentReplayService(IAgentStateStore store)
{
    public async Task<AgentReplayResult?> ReplayAsync(string key)
    {
        MarketAgentState? state;

        try
        {
            state = await store.GetAsync(key);
        }
        catch (Exception ex)
        {
            return new AgentReplayResult(
                key,
                [new(DateTimeOffset.UtcNow, "replay_error", "", ex.Message)],
                false);
        }

        if (state is null)
            return null;

        var timeline = state.Steps
            .OrderBy(s => s.Timestamp)
            .Select(s => new AgentStepView(
                s.Timestamp,
                s.StepName,
                s.Input,
                s.Output))
            .ToList();

        return new AgentReplayResult(
            Key: key,
            Timeline: timeline,
            Completed: state.Completed);
    }
}