namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public interface IReasoningAgentResolver
{
    IReasoningAgent GetPrimary();
    IReasoningAgent? GetFallback();
}