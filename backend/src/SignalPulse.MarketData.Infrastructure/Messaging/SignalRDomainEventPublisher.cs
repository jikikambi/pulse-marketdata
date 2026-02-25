using Microsoft.AspNetCore.SignalR;
using SignalPulse.MarketData.Infrastructure.Hubs;
using SignalPulse.MarketData.Infrastructure.RedisStore;

namespace SignalPulse.MarketData.Infrastructure.Messaging;

/// <summary>
/// Publishes domain events to clients via SignalR.
/// </summary>
public sealed class SignalRDomainEventPublisher(IHubContext<SignalPulseHub> hubCtx,
    IEventSequenceStore seqStore) : IDomainEventPublisher
{
    public async Task PublishAsync(string eventType, Guid eventId, object payload, DateTimeOffset timestamp, CancellationToken ct = default)
    {
        var sequence = await seqStore.GetNextAsync(ct).ConfigureAwait(false);

        var evt = new SignalREvent(EventId: eventId, Type: eventType, Payload: payload, Timestamp: timestamp, Sequence: sequence);

        if (payload is not null && payload.GetType().GetProperty("ClientId") is not null &&
            Guid.TryParse(payload.GetType().GetProperty("ClientId")!.GetValue(payload)?.ToString(), out var clientId))
        {
            await hubCtx.Clients.Group(clientId.ToString()).SendAsync(eventType, evt, ct);
        }

        await hubCtx.Clients.All.SendAsync(eventType, evt, ct);
    }
}