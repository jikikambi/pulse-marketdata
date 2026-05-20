using Microsoft.SemanticKernel;
using SignalPulse.MarketData.Application.AI.Models;
namespace SignalPulse.MarketData.Application.AI.Services.Providers;

public interface IKernelInvoker
{
    Task<string> InvokeAsync(string skillName, KernelArguments args, CancellationToken ct = default);
}