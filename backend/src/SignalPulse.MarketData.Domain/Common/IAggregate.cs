using SignalPulse.Abstractions.Events;

namespace SignalPulse.MarketData.Domain.Common;

public interface IAggregate
{
    Guid Id { get; }

    /// <summary>
    /// Returns domain events that have been raised but not yet persisted.
    /// </summary>
    /// <returns></returns>
    IReadOnlyList<IDomainEvent> GetUnCommittedEvents();

    /// <summary>
    /// Clears uncommitted events after persistence.
    /// </summary>
    void ClearUnCommittedEvents();
}
