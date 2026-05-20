using Microsoft.SemanticKernel;
using SignalPulse.MarketData.Application.AI.Skills.Services;
namespace SignalPulse.MarketData.Application.AI.Services.Providers;

public sealed class SemanticKernelInvoker(Kernel kernel,
    ISkillRegistry skillRegistry) : IKernelInvoker
{
    public async Task<string> InvokeAsync(string skillName, KernelArguments args, CancellationToken ct = default)
    {
        var skill = skillRegistry.Get(skillName);

        var result = await kernel.InvokeAsync(skill, args, ct);

        return result.ToString();
    }
}