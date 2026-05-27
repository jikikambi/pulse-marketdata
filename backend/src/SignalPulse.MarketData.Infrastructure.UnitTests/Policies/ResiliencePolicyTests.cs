using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace SignalPulse.MarketData.Infrastructure.UnitTests.Policies;

public class ResiliencePolicyTests
{
    [Fact]
    public async Task RetryPolicy_Should_Retry_Expected_Number_Of_Times()
    {
        // Arrange
        var logger = NullLogger.Instance;

        var policy = Policy.Handle<HttpRequestException>().WaitAndRetryAsync(3, _ => TimeSpan.Zero);

        var injector = new TestFailureInjector(3);

        var attempts = 0;

        // Act
        await policy.ExecuteAsync(async () =>
        {
            attempts++;

            await injector.ExecuteAsync();
        });

        // Assert
        attempts.Should().Be(4);
    }

    [Fact]
    public async Task CircuitBreaker_Should_Open_After_Configured_Failures()
    {
        // Arrange
        var breaker = Policy.Handle<HttpRequestException>()
            .CircuitBreakerAsync(exceptionsAllowedBeforeBreaking: 2, durationOfBreak: TimeSpan.FromSeconds(30));

        // Act
        await Assert.ThrowsAsync<HttpRequestException>(() => breaker.ExecuteAsync(() => throw new HttpRequestException()));

        await Assert.ThrowsAsync<HttpRequestException>(() => breaker.ExecuteAsync(() => throw new HttpRequestException()));

        // Assert
        await Assert.ThrowsAsync<BrokenCircuitException>(() => breaker.ExecuteAsync(() => Task.CompletedTask));
    }

    [Fact]
    public async Task TimeoutPolicy_Should_Cancel_Long_Running_Operation()
    {
        // Arrange
        var timeout = Policy.TimeoutAsync(TimeSpan.FromMilliseconds(100));

        // Act + Assert
        await Assert.ThrowsAsync<TimeoutRejectedException>(() =>
            timeout.ExecuteAsync(async ct =>
            {
                await Task.Delay(1000, ct);
            }, CancellationToken.None));
    }

    [Fact]
    public async Task CircuitBreaker_Should_Reset_After_Break_Duration()
    {
        // Arrange
        var breaker = Policy.Handle<HttpRequestException>()
            .CircuitBreakerAsync(1, TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAsync<HttpRequestException>(() => breaker.ExecuteAsync(() => throw new HttpRequestException()));

        await Assert.ThrowsAsync<BrokenCircuitException>(() => breaker.ExecuteAsync(() => Task.CompletedTask));

        await Task.Delay(300);

        await breaker.ExecuteAsync(() => Task.CompletedTask);
    }
}
