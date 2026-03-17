using FluentAssertions;
using SignalPulse.Abstractions.Events;

namespace SignalPulse.MarketData.Domain.UnitTests.Common;

public class AggregateRootTests
{
    [Fact]
    public void RaiseEvent_Should_Invoke_Handler_And_Add_To_Uncommitted()
    {
        var agg = new TestAggregate();

        agg.DoSomething();

        agg.Counter.Should().Be(1);

        var evt = agg.GetUnCommittedEvents().Single();
        evt.Should().BeOfType<TestEvent>();
    }

    [Fact]
    public void RaiseEvent_Should_Set_AggregateId_When_Empty()
    {
        var agg = new TestAggregate();

        agg.DoSomething();

        var evt = agg.GetUnCommittedEvents().Single();

        evt.AggregateId.Should().Be(agg.Id);
    }

    [Fact]
    public void RaiseEvent_Should_Not_Override_AggregateId_If_Already_Set()
    {
        var agg = new TestAggregate();
        var customId = Guid.NewGuid();

        var evt = new TestEvent { AggregateId = customId };

        // call protected via helper
        typeof(TestAggregate)
            .GetMethod("RaiseEvent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(agg, [evt]);

        agg.GetUnCommittedEvents().Single().AggregateId.Should().Be(customId);
    }

    [Fact]
    public void ApplyHistory_Should_Invoke_Handler_But_Not_Add_To_Uncommitted()
    {
        var agg = new TestAggregate();

        var events = new IDomainEvent[]
        {
            new TestEvent(),
            new AnotherTestEvent()
        };

        agg.ApplyHistory(events);

        agg.Counter.Should().Be(11);
        agg.GetUnCommittedEvents().Should().BeEmpty();
    }

    [Fact]
    public void ClearUnCommittedEvents_Should_Remove_All_Events()
    {
        var agg = new TestAggregate();

        agg.DoSomething();
        agg.DoAnother();

        agg.GetUnCommittedEvents().Should().HaveCount(2);

        agg.ClearUnCommittedEvents();

        agg.GetUnCommittedEvents().Should().BeEmpty();
    }

    [Fact]
    public void Mutate_Should_Throw_When_Handler_Not_Found()
    {
        var agg = new NoHandlerAggregate();

        Action act = () => agg.Trigger();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*does not handle*");
    }

    [Fact]
    public void RaiseEvent_Should_Invoke_Correct_Handler_For_Different_Events()
    {
        var agg = new TestAggregate();

        agg.DoSomething();   // +1
        agg.DoAnother();     // +10

        agg.Counter.Should().Be(11);
    }
}