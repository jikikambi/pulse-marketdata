namespace SignalPulse.MarketData.Application.AI.Models.Enums;

public enum MarketAgentStage
{
    ValidationInput = 0,
    Planning = 1,
    PlanParsing = 2,
    Tooling = 3,
    Reasoning = 4,
    Validation = 5,
    RiskEvaluation = 6,
    Scoring = 7,
    Decision = 8,
    Persistence = 9
}