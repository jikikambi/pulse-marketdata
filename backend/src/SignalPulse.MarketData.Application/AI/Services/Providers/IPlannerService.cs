using Microsoft.SemanticKernel;
namespace SignalPulse.MarketData.Application.AI.Services.Providers;

public interface IKernelInvoker
{
    Task<string> InvokeAsync(string skillName, KernelArguments args, CancellationToken ct = default);
}