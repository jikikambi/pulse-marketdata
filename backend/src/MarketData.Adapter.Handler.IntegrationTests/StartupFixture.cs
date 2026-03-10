using MarketData.Adapter.Api.Client;
using MarketData.Adapter.Handler.Handlers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MarketData.Adapter.Handler.IntegrationTests;

public class StartupFixture : IAsyncLifetime
{
    public IServiceProvider ServiceProvider { get; private set; } = default!;
    public ITestHarness Harness { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        // Build the configuration
        var configuration = new ConfigurationBuilder()
           .SetBasePath(AppContext.BaseDirectory)
           .AddJsonFile("appsettings.json", optional: true)
           .AddJsonFile("appsettings.Development.json", optional: true)
           .AddEnvironmentVariables()
           .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        // MassTransit Test Harness
        services.AddMassTransitTestHarness();

        services.AddHandlerServices(configuration);
        services.AddAlphaVantageApi(configuration);
        services.AddScoped<QuotePollingWorker>();

        // Build the service provider
        ServiceProvider = services.BuildServiceProvider(true);

        // Start the MassTransit test harness
        Harness = ServiceProvider.GetRequiredService<ITestHarness>();
        Harness.Start().Wait(); 
    }

    public Task DisposeAsync()
    {
        // Stop MassTransit harness asynchronously
        var stopHarness = Harness?.Stop() ?? Task.CompletedTask;

        // Dispose service provider
        if (ServiceProvider is IDisposable disposable)
            disposable.Dispose();

        return stopHarness;
    }
}
