using MassTransit;
using SignalPulse.Common;

namespace MarketData.Adapter.Handler;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddSignalPulseHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var postgres = configuration.GetValue<string>("Postgres:ConnectionString") ?? SignalPulseConstants.PostgresConnection;
        var redis = configuration.GetValue<string>("Redis:Connection") ?? SignalPulseConstants.RedisConnection;

        services.AddHealthChecks()
            .AddNpgSql(postgres!, name: "postgres", tags: ["db"])
            .AddRedis(redis!, name: "redis", tags: ["cache"]);

        return services;
    }
}