using Polly;

namespace SignalPulse.MarketData.Infrastructure.Policies;

public interface IAiPolicyRegistry
{
    IAsyncPolicy GetPlannerPolicy();
    IAsyncPolicy GetReasonerPolicy();
    IAsyncPolicy GetToolingPolicy();
    IAsyncPolicy GetValidationPolicy();
    IAsyncPolicy GetDecisionPolicy();
    IAsyncPolicy GetElasticPolicy();
    IAsyncPolicy GetDataAccessPolicy();
}