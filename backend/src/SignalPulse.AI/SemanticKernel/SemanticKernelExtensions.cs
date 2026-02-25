
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace SignalPulse.AI.SemanticKernel;

public static class SemanticKernelExtensions
{
    public static IServiceCollection AddPulseSemanticKernel(this IServiceCollection services, Action<IKernelBuilder>? customize = null)
    {
        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<ModelSecretsOptions>>().Value;

            var kernelBuilder = Kernel.CreateBuilder();

            kernelBuilder.AddOpenAIChatCompletion(modelId: opts.ModelId, apiKey: opts.OpenAIApiKey);

            customize?.Invoke(kernelBuilder);

            return kernelBuilder.Build();
        });

        return services;
    }
}
