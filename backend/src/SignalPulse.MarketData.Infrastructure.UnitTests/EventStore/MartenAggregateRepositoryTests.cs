using FakeItEasy;
using FluentAssertions;
using JasperFx.Events;
using Marten;
using Marten.Events;
using SignalPulse.Abstractions.Events;
using SignalPulse.MarketData.Domain.Common;
using SignalPulse.MarketData.Infrastructure.EventStore;

namespace SignalPulse.MarketData.Infrastructure.UnitTests.EventStore;

public class MartenAggregateRepositoryTests
{
    private readonly IDocumentSession _session = A.Fake<IDocumentSession>();
    private readonly IEventStoreOperations _events = A.Fake<IEventStoreOperations>();
    private readonly MartenAggregateRepository _repository;

    public MartenAggregateRepositoryTests()
    {
        A.CallTo(() => _session.Events).Returns(_events);
        _repository = new MartenAggregateRepository(_session);
    }

    private class TestAggregate : AggregateRoot
    {
        public int Counter { get; private set; }

        public void Increment() => RaiseEvent(new TestEvent());

        private void On(TestEvent e) => Counter++;
    }

    private class TestEvent : IDomainEvent
    {
        public Guid AggregateId { get; set; }
        public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    }

    [Fact]
    public async Task LoadAsync_Should_Return_Null_When_No_Events()
    {
        // Arrange
        A.CallTo(() => _events.FetchStreamAsync(A<Guid>.Ignored, A<long>.Ignored, A<DateTimeOffset?>.Ignored, A<long>.Ignored, A<CancellationToken>.Ignored))
            .Returns(Task.FromResult((IReadOnlyList<IEvent>)Array.Empty<IEvent>()));

        // Act
        var result = await _repository.LoadAsync<TestAggregate>(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadAsync_Should_Rehydrate_Aggregate_From_Events()
    {
        // Arrange
        var evt = new TestEvent { AggregateId = Guid.NewGuid() };
        var martensEvent = new Event<TestEvent>(evt) { Id = Guid.NewGuid(), Version = 1 };
        IReadOnlyList<IEvent> stream = [martensEvent];

        A.CallTo(() => _events.FetchStreamAsync(A<Guid>.Ignored, A<long>.Ignored, A<DateTimeOffset?>.Ignored, A<long>.Ignored, A<CancellationToken>.Ignored))
            .Returns(Task.FromResult(stream));

        // Act
        var aggregate = await _repository.LoadAsync<TestAggregate>(evt.AggregateId, CancellationToken.None);

        // Assert
        aggregate.Should().NotBeNull();
        aggregate!.Counter.Should().Be(1);
        aggregate.GetUnCommittedEvents().Should().BeEmpty();
    }

    [Fact]
    public async Task PersistAsync_Should_Persist_Events_And_Clear_Uncommitted()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.Increment();

        // Fake Append + SaveChangesAsync to succeed
        A.CallTo(() => _events.Append(aggregate.Id, A<IEnumerable<object>>.Ignored))
            .Returns(new StreamAction(aggregate.Id, StreamActionType.Append));

        A.CallTo(() => _session.SaveChangesAsync(A<CancellationToken>.Ignored))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _repository.PersistAsync(aggregate, CancellationToken.None);

        // Assert: uncommitted events are cleared
        aggregate.GetUnCommittedEvents().Should().BeEmpty();

        // Assert: persisted successfully
        result.Committed.Should().BeTrue();

        // Assert: returned metadata contains 1 event
        result.Events.Should().HaveCount(1);
        result.Events.First().Event.Should().BeOfType<TestEvent>();
    }

    [Fact]
    public async Task PersistAsync_Should_Return_False_When_SaveChanges_Fails()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.Increment();

        A.CallTo(() => _events.Append(aggregate.Id, A<IEnumerable<object>>.Ignored)).Returns(new StreamAction(aggregate.Id, StreamActionType.Append));

        A.CallTo(() => _session.SaveChangesAsync(A<CancellationToken>.Ignored)).ThrowsAsync(new InvalidOperationException());

        // Act
        var result = await _repository.PersistAsync(aggregate, CancellationToken.None);

        // Assert
        result.Committed.Should().BeFalse();
        result.Events.Should().BeEmpty();
        aggregate.GetUnCommittedEvents().Should().HaveCount(1);
    }
}