using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Options;
using SignalPulse.MarketData.Infrastructure.RedisStore;
using SignalPulse.Persistence.Redis;
using StackExchange.Redis;

namespace SignalPulse.MarketData.Infrastructure.UnitTests.RedisStore;

public class RedisIdempotencyStoreTests
{
    private readonly IConnectionMultiplexer _mux;
    private readonly IDatabase _db;
    private readonly RedisIdempotencyStore _store;

    public RedisIdempotencyStoreTests()
    {
        _db = A.Fake<IDatabase>();
        _mux = A.Fake<IConnectionMultiplexer>();
        A.CallTo(() => _mux.GetDatabase(A<int>._, A<object>._)).Returns(_db);

        var opts = Options.Create(new IdempotencyOptions
        {
            KeyPrefix = "idem:",
            EntryTtl = TimeSpan.FromHours(24)
        });

        _store = new RedisIdempotencyStore(_mux, opts);
    }

    [Fact]
    public async Task TryMarkProcessedAsync_ShouldReturnFalse_WhenKeyAlreadyExists()
    {
        // Arrange
        A.CallTo(() => _db.StringSetAsync( A<RedisKey>._, A<RedisValue>._, A<TimeSpan?>._, A<When>._, A<CommandFlags>._))
            .Returns(false);

        // Act
        var result = await _store.TryMarkProcessedAsync("cmd123", CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryMarkProcessedAsync_ShouldThrow_WhenKeyIsNullOrWhitespace()
    {
        // Act
        Func<Task> actNull = async () => await _store.TryMarkProcessedAsync(null!, CancellationToken.None);
        Func<Task> actEmpty = async () => await _store.TryMarkProcessedAsync("", CancellationToken.None);
        Func<Task> actWhitespace = async () => await _store.TryMarkProcessedAsync("   ", CancellationToken.None);

        // Assert
        await actNull.Should().ThrowAsync<ArgumentNullException>();
        await actEmpty.Should().ThrowAsync<ArgumentNullException>();
        await actWhitespace.Should().ThrowAsync<ArgumentNullException>();
    }
}