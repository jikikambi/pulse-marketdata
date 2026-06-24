using Microsoft.SemanticKernel;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Services.Providers;
using SignalPulse.MarketData.Infrastructure.Policies;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public class SemanticKernelReasoningAgent(IKernelInvoker kernelInvoker,
    IAiPolicyRegistry policyRegistry) : IReasoningAgent
{
    public string Name => "semantic_kernel";

    public async Task<string?> GenerateAsync(QuoteInsightInput input, string? toolContext, CancellationToken ct)
    {
        var retryPolicy = policyRegistry.GetReasonerPolicy();

        return await retryPolicy.ExecuteAsync(async () =>
        {
            var result = await kernelInvoker.InvokeAsync(AgentConstants.ReasonerSkill, new KernelArguments
            {
                ["symbol"] = input.Symbol,
                ["price"] = input.Price,
                ["changePercent"] = input.ChangePercent,
                ["volume"] = input.Volume,
                ["context"] = toolContext ?? "null",
                ["correlationId"] = input.CorrelationId
            }, ct);

            return result?.ToString();
        });
    }
}