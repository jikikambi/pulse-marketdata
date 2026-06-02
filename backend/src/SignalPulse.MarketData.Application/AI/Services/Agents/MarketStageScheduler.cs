using Microsoft.Extensions.Options;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public sealed class MarketStageScheduler(IOptions<MarketAgentOptions> options) : IMarketStageScheduler
{
    private readonly Dictionary<MarketAgentStage, IMarketAgentStage> _stageMap = [];
    private readonly Dictionary<MarketAgentStage, StageExecutionStatus> _status = [];
    private readonly HashSet<MarketAgentStage> _inProgress = [];

    public bool HasRemainingStages => _status.Values.Any(x => x == StageExecutionStatus.Pending) || _inProgress.Count > 0;

    public void Initialize(IReadOnlyList<IMarketAgentStage> stages)
    {
        _stageMap.Clear();
        _status.Clear();
        _inProgress.Clear();

        foreach (var stage in stages)
        {
            _stageMap[stage.Stage] = stage;
            _status[stage.Stage] = StageExecutionStatus.Pending;
        }
    }

    public IReadOnlyCollection<IMarketAgentStage> GetExecutableStages()
    {
        var availableSlots = Math.Max(0, options.Value.MaxParallelStages - _inProgress.Count);

        var ready = new List<IMarketAgentStage>();

        foreach (var stage in _stageMap.Values)
        {
            if (_status[stage.Stage] != StageExecutionStatus.Pending) continue;

            if (!stage.DependsOn.All(IsSatisfied)) continue;

            ready.Add(stage);

            if (ready.Count >= availableSlots) break;
        }

        if (ready.Count == 0 && _status.Values.Any(x => x == StageExecutionStatus.Pending))
        {
            throw new InvalidOperationException("Workflow deadlock detected. Pending stages have unsatisfied dependencies.");
        }

        foreach (var stage in ready)
        {
            _inProgress.Add(stage.Stage);
        }

        return ready;
    }

    public void MarkCompleted(MarketAgentStage stage)
    {
        _inProgress.Remove(stage);
        _status[stage] = StageExecutionStatus.Completed;
    }

    public void MarkSkipped(MarketAgentStage stage)
    {
        _inProgress.Remove(stage);
        _status[stage] = StageExecutionStatus.Skipped;
    }

    public void MarkFailed(MarketAgentStage stage)
    {
        _inProgress.Remove(stage);
        _status[stage] = StageExecutionStatus.Failed;
    }

    public StageExecutionStatus GetStatus(MarketAgentStage stage) => _status.GetValueOrDefault(stage);

    public IReadOnlyList<IReadOnlyList<IMarketAgentStage>> BuildExecutionPlan(IReadOnlyCollection<IMarketAgentStage> stages)
    {
        var remaining = stages.ToDictionary(s => s.Stage, s => new HashSet<MarketAgentStage>(s.DependsOn));

        var stageMap = stages.ToDictionary(s => s.Stage);

        var result = new List<IReadOnlyList<IMarketAgentStage>>();

        while (remaining.Count > 0)
        {
            var ready = remaining
                    .Where(x => x.Value.Count == 0)
                    .Select(x => stageMap[x.Key])
                    .ToList();

            if (ready.Count == 0)
            {
                throw new InvalidOperationException("Circular stage dependency detected.");
            }

            result.Add(ready);

            foreach (var stage in ready)
            {
                remaining.Remove(stage.Stage);

                foreach (var deps in remaining.Values)
                {
                    deps.Remove(stage.Stage);
                }
            }
        }

        return result;
    }

    private bool IsSatisfied(MarketAgentStage stage)
    {
        if (!_status.TryGetValue(stage, out var status))
        {
            return false;
        }

        return status is StageExecutionStatus.Completed or StageExecutionStatus.Skipped;
    }
}