using FakeItEasy;
using FluentAssertions;
using SignalPulse.MarketData.Infrastructure.RedisStore;
using StackExchange.Redis;

namespace SignalPulse.MarketData.Infrastructure.UnitTests.RedisStore;

public class RedisEventSequenceStoreTests
{
    private readonly IConnectionMultiplexer _mux;
    private readonly IDatabase _db;
    private readonly RedisEventSequenceStore _store;

    public RedisEventSequenceStoreTests()
    {
        _db = A.Fake<IDatabase>();
        _mux = A.Fake<IConnectionMultiplexer>();
        _store = new RedisEventSequenceStore(_mux, _db);
    }

    [Fact]
    public async Task GetNextAsync_Should_Call_StringIncrementAsync_And_ReturnValue()
    {
        // Arrange       
        A.CallTo(() => _mux.GetDatabase(default, default)).Returns(_db);

        var expected = 42L;
        A.CallTo(() => _db.StringIncrementAsync("event-sequence", 1, default)).Returns(Task.FromResult(expected));

        // Act
        var result = await _store.GetNextAsync();

        // Assert
        result.Should().Be(expected);
        A.CallTo(() => _db.StringIncrementAsync("event-sequence", 1, default)).MustHaveHappenedOnceExactly();
    }
}