using FluentAssertions;
using SignalPulse.MarketData.Contracts.Events;
using SignalPulse.MarketData.Domain.Exceptions;
using SignalPulse.MarketData.Domain.Quotes;

namespace SignalPulse.MarketData.Domain.UnitTests.Aggregates;

public class QuoteAggregateTests
{
    private static QuoteAggregate CreateValid() => QuoteAggregate.Create("AAPL", 150m, 2.5m);

    [Fact]
    public void Create_Should_Set_State_And_Raise_Event()
    {
        var agg = CreateValid();

        agg.Symbol.Should().Be("AAPL");
        agg.Price.Should().Be(150m);
        agg.ChangePercent.Should().Be(2.5m);

        var evt = agg.GetUnCommittedEvents().Single();
        evt.Should().BeOfType<QuoteCreated>();

        evt.AggregateId.Should().Be(agg.Id);
    }

    [Fact]
    public void Update_Should_Modify_State_And_Raise_Event()
    {
        var agg = CreateValid();
        agg.ClearUnCommittedEvents();

        agg.Update("AAPL", 155m, 3m);

        agg.Price.Should().Be(155m);

        var evt = agg.GetUnCommittedEvents().Single();
        evt.Should().BeOfType<QuoteUpdated>();
    }

    [Fact]
    public void Create_Should_Throw_When_Invalid_Data()
    {
        Action act = () => QuoteAggregate.Create("", 150m, 2m);

        act.Should().Throw<InvalidQuoteException>();
    }

    [Fact]
    public void Update_Should_Throw_When_Invalid_Data()
    {
        var agg = CreateValid();

        Action act = () => agg.Update("AAPL", -1m, 2m);

        act.Should().Throw<InvalidQuoteException>();
    }

    [Fact]
    public void ApplyHistory_Should_Rehydrate_State()
    {
        // Arrange
        var source = QuoteAggregate.Create("AAPL", 150m, 2m);
        source.Update("AAPL", 155m, 3m);

        var history = source.GetUnCommittedEvents();

        // Act
        var target = new QuoteAggregate();
        target.ApplyHistory(history);

        // Assert
        target.Symbol.Should().Be("AAPL");
        target.Price.Should().Be(155m);
        target.ChangePercent.Should().Be(3m);
    }

    [Fact]
    public void ApplyHistory_Should_Not_Add_Uncommitted_Events()
    {
        var created = new QuoteCreated("AAPL", 150m, 2m);
        created.GetType().GetProperty("AggregateId")!.SetValue(created, Guid.NewGuid());

        var agg = new QuoteAggregate();
        agg.ApplyHistory([created]);

        agg.GetUnCommittedEvents().Should().BeEmpty();
    }
}