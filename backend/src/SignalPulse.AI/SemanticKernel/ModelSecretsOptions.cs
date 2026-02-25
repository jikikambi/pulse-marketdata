namespace SignalPulse.AI.SemanticKernel;

public sealed class ModelSecretsOptions
{
    public string OpenAIApiKey { get; set; } = default!;
    public string ModelId { get; set; } = default!;
    public string AlphaVantageApiKey { get; set; } = default!;
}
