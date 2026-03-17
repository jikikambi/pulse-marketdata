using FluentAssertions;
using Microsoft.Extensions.Options;
using SignalPulse.MarketData.Infrastructure.RedisStore;
using SignalPulse.Persistence.Redis;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace SignalPulse.MarketData.Infrastructure.IntegrationTests.RedisStore;

public class RedisIdempotencyStoreTests : IAsyncLifetime
{
    private RedisContainer _redisContainer = null!;
    private IConnectionMultiplexer _mux = null!;
    private RedisIdempotencyStore _store = null!;

    public async Task InitializeAsync()
    {
        // Start a Redis test container
        _redisContainer = new RedisBuilder("redis:8-alpine")
            .WithImage("redis:8-alpine")
            .Build();

        await _redisContainer.StartAsync();

        // Connect to Redis
        _mux = await ConnectionMultiplexer.ConnectAsync(_redisContainer.GetConnectionString());

        var options = Options.Create(new IdempotencyOptions
        {
            KeyPrefix = "test:idem:",
            EntryTtl = TimeSpan.FromMinutes(5)
        });

        _store = new RedisIdempotencyStore(_mux, options);
    }

    public async Task DisposeAsync()
    {
        _store.Dispose();
        await _mux.CloseAsync();
        await _redisContainer.DisposeAsync();
    }

    [Fact]
    public async Task TryMarkProcessedAsync_ShouldReturnTrue_WhenKeyIsNew()
    {
        var key = Guid.NewGuid().ToString();
        var result = await _store.TryMarkProcessedAsync(key, CancellationToken.None);

        result.Should().BeTrue("the key was new and should be marked as processed");
    }

    [Fact]
    public async Task TryMarkProcessedAsync_ShouldReturnFalse_WhenKeyAlreadyExists()
    {
        var key = Guid.NewGuid().ToString();

        // First call sets it
        var first = await _store.TryMarkProcessedAsync(key, CancellationToken.None);
        first.Should().BeTrue();

        // Second call returns false
        var second = await _store.TryMarkProcessedAsync(key, CancellationToken.None);
        second.Should().BeFalse("the key already exists in Redis");
    }

    [Fact]
    public async Task TryMarkProcessedAsync_ShouldThrow_WhenKeyIsNullOrWhitespace()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _store.TryMarkProcessedAsync(null!, CancellationToken.None));

        await Assert.ThrowsAsync<ArgumentNullException>(() => _store.TryMarkProcessedAsync(string.Empty, CancellationToken.None));
    }
}