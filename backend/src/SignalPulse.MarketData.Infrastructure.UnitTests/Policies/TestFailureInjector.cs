namespace SignalPulse.MarketData.Infrastructure.UnitTests.Policies;

public sealed class TestFailureInjector(int failuresBeforeSuccess)
{
    private int _failuresRemaining = failuresBeforeSuccess;

    public Task ExecuteAsync()
    {
        if (_failuresRemaining > 0)
        {
            _failuresRemaining--;

            throw new HttpRequestException("Injected failure");
        }

        return Task.CompletedTask;
    }
}