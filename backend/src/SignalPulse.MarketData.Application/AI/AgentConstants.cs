using SignalPulse.MarketData.Application.AI.Plugins;

namespace SignalPulse.MarketData.Application.AI;

public static class AgentConstants
{
    public const string PromptPath = "AI/Prompts";

    public const string PlannerFunction = "Planner";
    public const string ReasonerFunction = "Reasoner";

    public const string ToolName = nameof(QuoteInfoPlugin.GetQuoteContextAsync);

    public const string StepPlanner = "planner";
    public const string StepTool = "tool_call";
    public const string StepReasoner = "reasoner";
    public const string StepSafe = "safe_fallback";
    public const string StepUnsafe = "unsafe_exit";
    public const string StepRisker = "risker";
}
