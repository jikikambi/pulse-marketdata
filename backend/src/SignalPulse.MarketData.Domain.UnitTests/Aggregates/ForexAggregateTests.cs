using FluentAssertions;
using SignalPulse.MarketData.Contracts.Events;
using SignalPulse.MarketData.Domain.Exceptions;
using SignalPulse.MarketData.Domain.Forex;

namespace SignalPulse.MarketData.Domain.UnitTests.Aggregates;

public class ForexAggregateTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static ForexAggregate CreateValid() => ForexAggregate.Create("EUR", "USD", 1.2m, 1.3m, 1.1m, 1.25m, Now);

    [Fact]
    public void Create_Should_Set_State_And_Raise_Event()
    {
        var agg = CreateValid();

        agg.FromSymbol.Should().Be("EUR");
        agg.ToSymbol.Should().Be("USD");
        agg.Open.Should().Be(1.2m);
        agg.High.Should().Be(1.3m);
        agg.Low.Should().Be(1.1m);
        agg.Close.Should().Be(1.25m);

        var evt = agg.GetUnCommittedEvents().Single();
        evt.Should().BeOfType<ForexCreated>();

        evt.AggregateId.Should().Be(agg.Id);
    }

    [Fact]
    public void Update_Should_Modify_State_And_Raise_Event()
    {
        var agg = CreateValid();
        agg.ClearUnCommittedEvents();

        agg.Update("EUR", "USD", 1.3m, 1.4m, 1.2m, 1.35m, Now);

        agg.Close.Should().Be(1.35m);
        agg.High.Should().Be(1.4m);

        var evt = agg.GetUnCommittedEvents().Single();

        evt.Should().BeOfType<ForexUpdated>();
    }

    [Fact]
    public void Create_Should_Throw_When_Invalid_Data()
    {
        // Act
        Action act = () => ForexAggregate.Create("EUR", "USD", 1m, 1m, 2m, 1m, Now); // high < low

        // Assert
        act.Should().Throw<InvalidForexException>();
    }

    [Fact]
    public void Update_Should_Throw_When_Invalid_Data()
    {
        // Arrange
        var agg = CreateValid();

        // Act
        Action act = () => agg.Update("EUR", "USD", 1m, 1m, 2m, 1m, Now);

        // Assert
        act.Should().Throw<InvalidForexException>();
    }

    [Fact]
    public void ApplyHistory_Should_Rehydrate_State()
    {
        // Arrange
        var source = ForexAggregate.Create("EUR", "USD", 1.2m, 1.3m, 1.1m, 1.25m, Now);
        source.Update("EUR", "USD", 1.3m, 1.4m, 1.2m, 1.35m, Now);

        var history = source.GetUnCommittedEvents();

        // Act
        var target = new ForexAggregate();
        target.ApplyHistory(history);

        // Assert
        target.FromSymbol.Should().Be("EUR");
        target.ToSymbol.Should().Be("USD");
        target.Open.Should().Be(1.3m);
        target.High.Should().Be(1.4m);
        target.Low.Should().Be(1.2m);
        target.Close.Should().Be(1.35m);
    }

    [Fact]
    public void ApplyHistory_Should_Not_Add_Uncommitted_Events()
    {
        // Arrange
        var created = new ForexCreated("EUR", "USD", 1.2m, 1.3m, 1.1m, 1.25m, Now);
        created.GetType().GetProperty("AggregateId")!.SetValue(created, Guid.NewGuid());

        // Act
        var agg = new ForexAggregate();
        agg.ApplyHistory([created]);

        // Assert
        agg.GetUnCommittedEvents().Should().BeEmpty();
    }
}