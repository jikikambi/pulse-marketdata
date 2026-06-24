using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public interface IPlannerExecutionAgent
{
    string Name { get; }
    Task<string?> GenerateAsync(QuoteInsightInput input, MarketAgentWorkflowContext ctx, MarketAgentStage stage, CancellationToken ct);
}