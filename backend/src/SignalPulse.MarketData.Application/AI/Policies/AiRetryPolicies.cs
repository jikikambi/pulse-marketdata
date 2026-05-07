using Polly;
using Polly.Retry;

namespace SignalPulse.MarketData.Application.AI.Policies;

public static class AiRetryPolicies
{
    public static AsyncRetryPolicy Create()
    {
        return Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retry =>
                    TimeSpan.FromSeconds(Math.Pow(2, retry)));
    }
}