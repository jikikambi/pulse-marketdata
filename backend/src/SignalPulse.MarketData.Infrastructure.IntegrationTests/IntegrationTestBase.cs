using Marten;

namespace SignalPulse.MarketData.Infrastructure.IntegrationTests;

public abstract class IntegrationTestBase(StartupFixture fixture)
{
    protected readonly IDocumentStore Store = fixture.Store;

    protected async Task CleanDatabase()
    {
        await Store.Advanced.Clean.DeleteAllDocumentsAsync();
    }
}