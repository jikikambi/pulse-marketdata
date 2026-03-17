using FluentAssertions;
using SignalPulse.MarketData.Infrastructure.RedisStore;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace SignalPulse.MarketData.Infrastructure.IntegrationTests.RedisStore;

public class RedisEventSequenceStoreIntegrationTests : IAsyncLifetime
{
    private RedisContainer _redisContainer = null!;
    private IConnectionMultiplexer _mux = null!;
    private RedisEventSequenceStore _store = null!;

    public async Task InitializeAsync()
    {
        _redisContainer = new RedisBuilder("redis:7-alpine")
            .WithPortBinding(6379, true)
            .Build();

        await _redisContainer.StartAsync();

        var options = ConfigurationOptions.Parse(_redisContainer.GetConnectionString());
        _mux = await ConnectionMultiplexer.ConnectAsync(options);

        _store = new RedisEventSequenceStore(_mux);
    }

    public async Task DisposeAsync()
    {
        await _mux.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }

    [Fact]
    public async Task GetNextAsync_Should_Increment_Sequence()
    {
        // Act
        var first = await _store.GetNextAsync();
        var second = await _store.GetNextAsync();

        // Assert
        second.Should().Be(first + 1);
    }
}
