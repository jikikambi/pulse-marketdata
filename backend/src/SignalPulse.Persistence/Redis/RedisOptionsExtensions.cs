using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace SignalPulse.Persistence.Redis;

public static class RedisOptionsExtensions
{
    public static IServiceCollection AddPulseRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConn = configuration.GetValue<string>("Redis:Connection") ?? "localhost:6379";

        services.Configure<IdempotencyOptions>(configuration.GetSection("Idempotency"));

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Redis");
            try
            {
                var opts = ConfigurationOptions.Parse(redisConn);
                opts.AbortOnConnectFail = false;
                opts.ConnectRetry = 3;
                opts.ConnectTimeout = 5000;
                return ConnectionMultiplexer.Connect(opts);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connect to Redis");
                throw;
            }
        });

        return services;
    }
}
