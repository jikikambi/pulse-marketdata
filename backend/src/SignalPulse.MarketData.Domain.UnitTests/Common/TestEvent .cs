using SignalPulse.Abstractions.Events;
using SignalPulse.MarketData.Domain.Common;

namespace SignalPulse.MarketData.Domain.UnitTests.Common;

internal class TestEvent : IDomainEvent
{
    public Guid AggregateId { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}

internal class AnotherTestEvent : IDomainEvent
{
    public Guid AggregateId { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}

internal class TestAggregate : AggregateRoot
{
    public int Counter { get; private set; }

    public void DoSomething()
    {
        RaiseEvent(new TestEvent());
    }

    public void DoAnother()
    {
        RaiseEvent(new AnotherTestEvent());
    }

    // Event handlers
    private void On(TestEvent e)
    {
        Counter++;
    }

    private void On(AnotherTestEvent e)
    {
        Counter += 10;
    }
}

internal class NoHandlerAggregate : AggregateRoot
{
    public void Trigger()
    {
        RaiseEvent(new TestEvent());
    }
}