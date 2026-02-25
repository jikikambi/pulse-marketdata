using JasperFx.Events.Projections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SignalPulse.Abstractions.Events;
using SignalPulse.MarketData.Contracts.Events;
using SignalPulse.MarketData.Domain.Common;
using SignalPulse.MarketData.Infrastructure.EventStore;
using SignalPulse.MarketData.Infrastructure.Messaging;
using SignalPulse.MarketData.Infrastructure.Persistence;
using SignalPulse.MarketData.Infrastructure.Projections;
using SignalPulse.MarketData.Infrastructure.ReadModels;
using SignalPulse.MarketData.Infrastructure.RedisStore;
using SignalPulse.Persistence.Marten;
using SignalPulse.Persistence.Redis;

namespace SignalPulse.MarketData.Infrastructure;

public static class MarketDataMartenExtensions
{
    public static IServiceCollection AddMarketDataRedisMarten(this IServiceCollection services, IConfiguration config)
    {
        // --- Redis / Idempotency ---       
        services.AddPulseRedis(config);
        services.AddSingleton<IIdempotencyStore, RedisIdempotencyStore>();

        services.AddScoped<IDomainEventPublisher, SignalRDomainEventPublisher>();

        // --- Marten / Postgres ---
        services.AddPulseMarten(config, opts =>
        {
            opts.Events.AddEventTypes([.. typeof(QuoteCreated)
                    .Assembly.GetTypes()
                    .Where(t => typeof(IDomainEvent).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)]
            );

            // --- Register projections ---
            opts.Projections.Add<QuoteProjection>(ProjectionLifecycle.Inline);
            opts.Projections.Add<QuoteInsightProjection>(ProjectionLifecycle.Inline);
        });           

        // --- Repositories ---
        services.AddScoped<IReadModelRepository<QuoteReadModel>, QuoteRepository>();
        services.AddScoped<IReadModelRepository<QuoteInsightReadModel>, QuoteInsightRepository>();

        // --- Domain infrastructure ---
        services.AddScoped<IAggregateRepository, MartenAggregateRepository>();

        return services;
    }
}