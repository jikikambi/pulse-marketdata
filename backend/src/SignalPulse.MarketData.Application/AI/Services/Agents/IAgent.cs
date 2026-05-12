namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public interface IAgent<in TInput, TResult>
{
    Task<TResult> ExecuteAsync(TInput input, CancellationToken ct);
}