using SignalPulse.MarketData.Application.AI.Models;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public interface IFailureClassifier
{
    FailureClassification Classify(IMarketAgentStage stage, Exception exception);
}