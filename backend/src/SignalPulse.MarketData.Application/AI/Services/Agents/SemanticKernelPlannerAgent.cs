using Microsoft.SemanticKernel;
using Polly;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.AI.Services.Providers;
using SignalPulse.MarketData.Infrastructure.Policies;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class SemanticKernelPlannerAgent(IKernelInvoker kernelInvoker,
    IAiPolicyRegistry policyRegistry) : IPlannerExecutionAgent
{
    public string Name => "semantic_kernel";

    public async Task<string?> GenerateAsync(QuoteInsightInput input, MarketAgentWorkflowContext ctx, MarketAgentStage stage,  CancellationToken ct)
    {
        var policyContext = new Context
        {
            ["emitter"] = ctx
        };

        var retryPolicy = policyRegistry.GetPlannerPolicy();

        return await retryPolicy.ExecuteAsync(async (_, token) =>
        {
            await ctx.EmitAsync(stage.ToString(), "planner_invocation", "Invoking planner model", null, token);

            var result = await kernelInvoker.InvokeAsync(AgentConstants.PlannerSkill, new KernelArguments
            {
                ["symbol"] = input.Symbol,
                ["price"] = input.Price,
                ["changePercent"] = input.ChangePercent,
                ["volume"] = input.Volume,
                ["correlationId"] = input.CorrelationId
            }, token);

            return result?.ToString();
        }, policyContext, ct);
    }
}