using SignalPulse.Abstractions.Events;

namespace SignalPulse.MarketData.Domain.Common;

public readonly record struct PersistResult(bool Committed, IReadOnlyList<EventMetadata> Events);