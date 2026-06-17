using System.Text.Json;
using System.Text.Json.Serialization;

namespace SignalPulse.MarketData.Application.AI.Services.Providers;

public static class AiJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };
}

public static class ReasoningAgentNames
{
    public const string Template = "template";
    public const string SemanticKernel = "semantic_kernel";
}