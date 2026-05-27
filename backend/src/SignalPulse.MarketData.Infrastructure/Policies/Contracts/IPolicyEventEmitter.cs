namespace SignalPulse.MarketData.Infrastructure.Policies.Contracts;

public interface IPolicyEventEmitter
{
    Task EmitAsync(string stage, string eventType, string message, object? metadata = null, CancellationToken cancellationToken = default);
}