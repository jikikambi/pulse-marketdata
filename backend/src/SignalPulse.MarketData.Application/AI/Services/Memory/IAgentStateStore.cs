using SignalPulse.MarketData.Application.AI.Models;

namespace SignalPulse.MarketData.Application.AI.Services.Memory;

public interface IAgentStateStore
{
    Task<MarketAgentState?> GetAsync(string key);
    Task SetAsync(string key, MarketAgentState state);
    Task DeleteAsync(string key);
    Task<IEnumerable<string>> GetKeysAsync(string pattern);
}