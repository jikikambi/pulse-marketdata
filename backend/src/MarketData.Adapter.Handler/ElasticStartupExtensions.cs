using SignalPulse.MarketData.Infrastructure.Elastic;

namespace MarketData.Adapter.Handler;

public static class ElasticStartupExtensions
{
    public static async Task InitializeElasticAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();

        var initializer = scope.ServiceProvider.GetRequiredService<ElasticIndexInitializer>();

        await initializer.InitializeAsync(ct);
    }
}