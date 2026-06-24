namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public interface IPlannerAgentResolver
{
    IPlannerExecutionAgent GetPrimary();
    IPlannerExecutionAgent? GetFallback();
}