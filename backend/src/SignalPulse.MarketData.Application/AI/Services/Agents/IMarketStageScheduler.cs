using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public interface IMarketStageScheduler
{
    bool HasRemainingStages { get; }
    void Initialize( IReadOnlyList<IMarketAgentStage> stages);
    IReadOnlyCollection<IMarketAgentStage> GetExecutableStages();
    IReadOnlyList<IReadOnlyList<IMarketAgentStage>> BuildExecutionPlan(IReadOnlyCollection<IMarketAgentStage> stages);
    void MarkCompleted( MarketAgentStage stage);
    void MarkSkipped( MarketAgentStage stage);
    void MarkFailed( MarketAgentStage stage);
    StageExecutionStatus GetStatus(MarketAgentStage stage);
}