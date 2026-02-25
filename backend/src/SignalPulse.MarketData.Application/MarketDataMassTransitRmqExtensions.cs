using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SignalPulse.Messaging.MassTransit;
using System.Reflection;

namespace SignalPulse.MarketData.Application;

public static class MarketDataMassTransitRmqExtensions
{
    public static IServiceCollection AddMarketDataMassTransitRMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPulseMassTransitMessging(configuration, bus =>
        {
            bus.AddConsumers(Assembly.GetExecutingAssembly());

            bus.AddInMemoryInboxOutbox();
        });
        return services;
    }
}