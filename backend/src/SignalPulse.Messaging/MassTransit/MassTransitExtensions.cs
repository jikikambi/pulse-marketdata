using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SignalPulse.Messaging.RabbitMq;

namespace SignalPulse.Messaging.MassTransit;

public static class MassTransitExtensions
{
    public static IServiceCollection AddPulseMassTransitMessging(this IServiceCollection services,
        IConfiguration configuration
        , Action<IBusRegistrationConfigurator>? customize = null)
    {
        var options = configuration.GetSection("RabbitMq").Get<RabbitMqOptions>() ?? throw new InvalidOperationException("RabbitMq options missing");

        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();

            // Consumer-level customization
            customize?.Invoke(bus);

            // --- Transport ---
            bus.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(new Uri(options.MqConnection));

                // Apply default endpoint conventions
                cfg.ConfigureEndpoints(ctx);
            });
        });
        return services;
    }
}
