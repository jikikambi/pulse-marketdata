using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;
using SignalPulse.MarketData.Infrastructure.Policies.Contracts;

namespace SignalPulse.MarketData.Infrastructure.Policies;

public sealed class AiPolicyRegistry(ILogger<AiPolicyRegistry> logger)
    : IAiPolicyRegistry
{

    private readonly IAsyncPolicy _plannerPolicy = CreateAiPolicy(operation: "planner",
        timeoutSeconds: 10,
        retryCount: 3,
        exceptionsAllowedBeforeBreaking: 5,
        durationOfBreakSeconds: 30,
        logger);

    private readonly IAsyncPolicy _reasonerPolicy = CreateAiPolicy(operation: "reasoner",
        timeoutSeconds: 15,
        retryCount: 2,
        exceptionsAllowedBeforeBreaking: 4,
        durationOfBreakSeconds: 45,
        logger);

    private readonly IAsyncPolicy _toolingPolicy = CreateAiPolicy(operation: "tooling",
        timeoutSeconds: 20,
        retryCount: 2,
        exceptionsAllowedBeforeBreaking: 5,
        durationOfBreakSeconds: 20,
        logger);

    private readonly IAsyncPolicy _validationPolicy = CreateAiPolicy(operation: "validation",
        timeoutSeconds: 10,
        retryCount: 1,
        exceptionsAllowedBeforeBreaking: 5,
        durationOfBreakSeconds: 15,
        logger);

    private readonly IAsyncPolicy _decisionPolicy = CreateAiPolicy(operation: "decision",
        timeoutSeconds: 10,
        retryCount: 1,
        exceptionsAllowedBeforeBreaking: 5,
        durationOfBreakSeconds: 15,
        logger);

    private readonly IAsyncPolicy _elasticPolicy = CreateElasticPolicy(logger);

    private readonly IAsyncPolicy _dataAccessPolicy = CreateDataAccessPolicy();

    public IAsyncPolicy GetDataAccessPolicy() => _dataAccessPolicy;
    public IAsyncPolicy GetPlannerPolicy() => _plannerPolicy;
    public IAsyncPolicy GetReasonerPolicy() => _reasonerPolicy;
    public IAsyncPolicy GetToolingPolicy() => _toolingPolicy;
    public IAsyncPolicy GetValidationPolicy() => _validationPolicy;
    public IAsyncPolicy GetDecisionPolicy() => _decisionPolicy;
    public IAsyncPolicy GetElasticPolicy() => _elasticPolicy;

    private static IAsyncPolicy CreateAiPolicy(string operation, int timeoutSeconds, int retryCount, int exceptionsAllowedBeforeBreaking, int durationOfBreakSeconds, ILogger logger)
    {
        var retry = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetryAsync: async (outcome, delay, retryAttempt, context) =>
                {
                    logger.LogWarning(outcome, "[{Operation}] Retry {RetryAttempt} after {DelayMs}ms", operation, retryAttempt, delay.TotalMilliseconds);

                    if (context.TryGetValue("emitter", out var workflowObj) && workflowObj is IPolicyEventEmitter emitter)
                    {
                        try
                        {
                            await emitter.EmitAsync(operation, $"{operation}_retry", $"{operation} retry attempt {retryAttempt}", new
                            {
                                Retry = retryAttempt,
                                DelayMs = delay.TotalMilliseconds,
                                Exception = outcome?.Message
                            });
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Emitter failed during retry event");
                        }
                    }
                });

        var breaker = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .Or<TaskCanceledException>()
            .CircuitBreakerAsync(exceptionsAllowedBeforeBreaking, TimeSpan.FromSeconds(durationOfBreakSeconds),
                onBreak: (outcome, duration, context) =>
                {
                    logger.LogError(outcome, "[{Operation}] Circuit OPEN for {DurationSeconds}s", operation, duration.TotalSeconds);

                    if (context.TryGetValue("emitter", out var emitterObj) && emitterObj is IPolicyEventEmitter emitter)
                    {
                        _ = SafeEmit(emitter, logger, operation, $"{operation}_circuit_open", "circuit opened", new
                        {
                            DurationSeconds = duration.TotalSeconds,
                            Exception = outcome?.Message
                        });
                    }
                },
                onReset: context =>
                {
                    logger.LogInformation("[{Operation}] Circuit RESET", operation);

                    if (context.TryGetValue("emitter", out var emitterObj) && emitterObj is IPolicyEventEmitter emitter)
                    {
                        _ = SafeEmit(emitter, logger, operation, $"{operation}_circuit_reset", "circuit reset");
                    }
                },
                onHalfOpen: () =>
                {
                    logger.LogWarning("[{Operation}] Circuit HALF-OPEN", operation);
                });

        var timeout = Policy.TimeoutAsync(TimeSpan.FromSeconds(timeoutSeconds));

        return Policy.WrapAsync(retry, breaker, timeout);
    }

    private static IAsyncPolicy CreateElasticPolicy(ILogger logger)
    {
        var retry = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(3,
                retryAttempt => TimeSpan.FromMilliseconds(500 * retryAttempt),
                onRetry: (exception, delay, retryAttempt, _) =>
                {
                    logger.LogWarning(exception, "[elastic] Retry {RetryAttempt} after {DelayMs}ms", retryAttempt, delay.TotalMilliseconds);
                });

        var breaker = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(20),
            onBreak: (outcome, duration, _) =>
            {
                logger.LogError(outcome.InnerException, "[elastic] Circuit OPEN for {DurationSeconds}s", duration.TotalSeconds);
            },
            onReset: _ =>
            {
                logger.LogInformation("[elastic] Circuit RESET");
            });

        var timeout = Policy.TimeoutAsync(TimeSpan.FromSeconds(5));

        return Policy.WrapAsync(retry, breaker, timeout);
    }

    private static IAsyncPolicy CreateDataAccessRetryPolicy() => Policy.Handle<Exception>(IsTransientDataException)
        .WaitAndRetryAsync(retryCount: 3, sleepDurationProvider: retry => TimeSpan.FromMilliseconds(250 * retry));

    private static IAsyncPolicy CreateDataAccessCircuitBreaker() => Policy.Handle<Exception>(IsTransientDataException)
        .CircuitBreakerAsync(exceptionsAllowedBeforeBreaking: 5, durationOfBreak: TimeSpan.FromSeconds(30));

    private static IAsyncPolicy CreateDataAccessTimeoutPolicy() => Policy.TimeoutAsync(TimeSpan.FromSeconds(5));

    private static IAsyncPolicy CreateDataAccessPolicy() => Policy.WrapAsync(
        CreateDataAccessRetryPolicy(),
        CreateDataAccessCircuitBreaker(),
        CreateDataAccessTimeoutPolicy());

    private static bool IsTransientDataException(Exception ex) => ex is TimeoutException
        or HttpRequestException
        or TaskCanceledException
        || ex.GetType().Name.Contains("Sql")
        || ex.GetType().Name.Contains("Npgsql")
        || ex.GetType().Name.Contains("Mongo")
        || ex.GetType().Name.Contains("Redis");
        
    private static async Task SafeEmit(IPolicyEventEmitter emitter, ILogger logger, string operation, string eventName, string message, object? data = null)
    {
        try
        {
            await emitter.EmitAsync(operation, eventName, message, data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Emitter failed: {Event}", eventName);
        }
    }
}