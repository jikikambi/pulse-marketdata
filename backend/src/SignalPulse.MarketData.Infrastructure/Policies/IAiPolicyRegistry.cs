using Polly;

namespace SignalPulse.MarketData.Infrastructure.Policies;

public interface IAiPolicyRegistry
{
    IAsyncPolicy<string> GetPlannerPolicy();
    IAsyncPolicy<string> GetReasonerPolicy();
    IAsyncPolicy GetElasticPolicy();
    IAsyncPolicy GetDataAccessPolicy();
}