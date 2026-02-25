using SignalPulse.Abstractions.Events;
using System.Collections.Concurrent;
using System.Reflection;

namespace SignalPulse.MarketData.Domain.Common;

public class AggregateRoot : IAggregate
{
    private readonly List<IDomainEvent> _uncommittedEvents = [];
    private static readonly ConcurrentDictionary<(Type, Type), MethodInfo> _handlerCache = [];

    public Guid Id { get; protected set; }

    public IReadOnlyList<IDomainEvent> GetUnCommittedEvents() => _uncommittedEvents.AsReadOnly();

    public void ClearUnCommittedEvents() => _uncommittedEvents.Clear();

    /// <summary>
    /// Rehydrates the aggregate by applying historical events.
    /// </summary>
    /// <param name="events"></param>
    public void ApplyHistory(IEnumerable<IDomainEvent> events)
    {
        foreach (var evt in events)
            Mutate(evt);
    }

    /// <summary>
    /// Public method for raising new events.
    /// 1. Apply state changes
    /// 2. Mark as unpersisted
    /// </summary>
    /// <param name="evt"></param>
    protected void RaiseEvent(IDomainEvent evt)
    {
        if (evt.AggregateId == Guid.Empty)
        {
            evt.GetType().GetProperty(nameof(IDomainEvent.AggregateId))!.SetValue(evt, Id);
        }

        Mutate(evt);
        _uncommittedEvents?.Add(evt);
    }

    /// <summary>
    /// Internal dispatcher for calling the correct event handler.
    /// Follows the On(EventType e) naming convention.
    /// </summary>
    /// <param name="evt"></param>
    private void Mutate(IDomainEvent evt)
    {
        var key = (GetType(), evt.GetType());

        var handler = _handlerCache.GetOrAdd(key, static key => 
        {
            var (aggType, evtType) = key;

            var handler = aggType.GetMethod("On", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [evtType], null);

            return handler is null ? throw new InvalidOperationException($"{aggType.Name} does not handle {evtType.GetType().Name}") : handler;
        });

        handler.Invoke(this, [evt]);
    }
}
