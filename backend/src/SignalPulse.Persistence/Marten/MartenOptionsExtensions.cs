using JasperFx;
using JasperFx.Events;
using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SignalPulse.Persistence.Postgres;
using Wolverine.Marten;

namespace SignalPulse.Persistence.Marten;

public static class MartenOptionsExtensions
{
    public static IServiceCollection AddPulseMarten(this IServiceCollection services, IConfiguration config, Action<StoreOptions>? customize = null)
    {
        var postgresConn = config.GetSection("Postgres").Get<PostgresOptions>() ?? throw new InvalidOperationException("Postgres connection string is not configured");

        services.AddMarten(opts =>
        {
            opts.Connection(postgresConn.ConnectionString);
            opts.DatabaseSchemaName = "marketdata";
            opts.AutoCreateSchemaObjects = AutoCreate.All;

            // Event sourcing defaults
            opts.Events.StreamIdentity = StreamIdentity.AsGuid;
            opts.Events.MetadataConfig.HeadersEnabled = true;            
            
            customize?.Invoke(opts);
        })
            .IntegrateWithWolverine()
            .ApplyAllDatabaseChangesOnStartup();

        services.AddScoped(sp => sp.GetRequiredService<IDocumentStore>().LightweightSession());
        services.AddScoped(sp => sp.GetRequiredService<IDocumentStore>().QuerySession());

        return services;
    }    
}

