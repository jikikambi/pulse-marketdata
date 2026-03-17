using JasperFx;
using JasperFx.Events;
using Marten;
using SignalPulse.Abstractions.Events;
using SignalPulse.MarketData.Contracts.Events;
using Testcontainers.PostgreSql;

namespace SignalPulse.MarketData.Infrastructure.IntegrationTests;

public class StartupFixture : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;
    public IDocumentStore Store { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder("postgres:16-alpine")
             .WithDatabase("testdb")
             .WithUsername("postgres")
             .WithPassword("postgres")
             .Build();

        await _container.StartAsync();

        Store = DocumentStore.For(opts =>
        {
            opts.Connection(_container.GetConnectionString());
            opts.DatabaseSchemaName = "testmarketdata";
            opts.AutoCreateSchemaObjects = AutoCreate.All;

            // Event sourcing defaults
            opts.Events.StreamIdentity = StreamIdentity.AsGuid;
            opts.Events.MetadataConfig.HeadersEnabled = true;

            opts.Events.AddEventTypes([.. typeof(QuoteCreated)
                    .Assembly.GetTypes()
                    .Where(t => typeof(IDomainEvent).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)]
            );

            // Only read models — no projections registered            
        });
    }

    public async Task DisposeAsync()
    {
        await Store.DisposeAsync();
        await _container.DisposeAsync();
    }
}