using MarketData.Adapter.Handler.Handlers;
using Prometheus;

namespace MarketData.Adapter.Handler;

public static class PrometheusExtensions
{
    /// <summary>
    /// Registers Prometheus + .NET runtime metrics + custom hosted services.
    /// </summary>
    public static IServiceCollection AddSignalPulsePrometheus(this IServiceCollection services)
    {
        // custom hosted service
        services.AddHostedService<QuotePollingWorker>();
        services.AddHostedService<ForexPollingWorker>();

        return services;
    }

    /// <summary>
    /// Enables Prometheus metric endpoints and HTTP metrics middleware.
    /// </summary>
    public static WebApplication UseSignalPulseObservability(this WebApplication app)
    {
        // /metrics endpoint
        app.MapPrometheusScrapingEndpoint();

        app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description,
                        duration = entry.Value.Duration.TotalMilliseconds
                    })
                };
                await context.Response.WriteAsJsonAsync(result);
            }
        });

        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false
        });

        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        // HTTP request duration/count/status
        app.UseHttpMetrics();

        return app;
    }
}
