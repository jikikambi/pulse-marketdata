using Microsoft.SemanticKernel;
namespace SignalPulse.MarketData.Application.AI.Services.Providers;

public sealed class SemanticKernelInvoker(Kernel kernel) : IKernelInvoker
{
    private readonly string _promptPath = Path.Combine(AppContext.BaseDirectory, AgentConstants.PromptPath);

    public async Task<string> InvokeAsync(string functionName, KernelArguments args, CancellationToken ct = default)
    {
        var plugin = kernel.CreatePluginFromPromptDirectory(_promptPath);

        var result = await kernel.InvokeAsync(plugin[functionName], args, ct);

        return result.ToString();
    }
}