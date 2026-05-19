using Polly;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Policies;

public static class AiRetryPolicies
{
    public static IAsyncPolicy<string> Create()
    {
        return Policy<string>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(retryCount: 3,
            sleepDurationProvider: retry => TimeSpan.FromSeconds(Math.Pow(2, retry)),
            onRetryAsync: async (outcome, delay, retryCount, context) =>
            {
                if (context.TryGetValue("workflowContext", out var workflowObj) && workflowObj is MarketAgentWorkflowContext workflowCtx)
                {
                    await workflowCtx.EmitAsync(MarketAgentStage.Planning.ToString(), "planner_retry", $"Planner retry attempt {retryCount}", new
                    {
                        Retry = retryCount,
                        DelayMs = delay.TotalMilliseconds,
                        Exception = outcome.Exception.Message
                    });
                }
            });
    }
}