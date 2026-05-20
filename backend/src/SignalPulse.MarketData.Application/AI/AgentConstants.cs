using SignalPulse.MarketData.Application.AI.Plugins;

namespace SignalPulse.MarketData.Application.AI;

public static class AgentConstants
{
    public const string SkillsPath = "AI/Skills";

    public const string PlannerSkill = "MarketPlanner";
    public const string ReasonerSkill = "MarketReasoner";

    public const string QuoteInsightSkill = "QuoteInsight";
    public const string ForexInsightSkill = "ForexInsight";

    public const string ToolName = nameof(QuoteInfoPlugin.GetQuoteContextAsync);

    public const string StepPlanner = "planner";
    public const string StepTool = "tool_call";
    public const string StepReasoner = "reasoner";
    public const string StepSafe = "safe_fallback";
    public const string StepUnsafe = "unsafe_exit";
    public const string StepRisker = "risker";
    public const string StepInputValidation = "input_validation";    
}
