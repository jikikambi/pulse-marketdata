using Elastic.Clients.Elasticsearch;
using FluentAssertions;
using Microsoft.Extensions.Options;
using SignalPulse.MarketData.Infrastructure.Elastic;

namespace SignalPulse.MarketData.Infrastructure.IntegrationTests.Elastic;

public class ElasticIndexInitializerTests
{
    [Fact]
    public async Task InitializeAsync_Should_Create_Workflow_Index_Template()
    {
        // Arrange
        var prefix = $"workflow-events-{Guid.NewGuid():N}";

        var client = new ElasticsearchClient(new ElasticsearchClientSettings(new Uri("http://localhost:9200")));

        var initializer = new ElasticIndexInitializer(client,
            Options.Create(new ElasticOptions
            {
                IndexPrefix = prefix
            }));

        // Act
        await initializer.InitializeAsync();

        var response = await client.Indices.GetIndexTemplateAsync( $"{prefix}-template");

        // Assert
        response.IsValidResponse.Should().BeTrue();
        response.IndexTemplates.Should().NotBeEmpty();
    }
}