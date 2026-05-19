using Elastic.Clients.Elasticsearch;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SignalPulse.MarketData.Infrastructure.Elastic;

namespace SignalPulse.MarketData.Application.UnitTests.Elastic;

public class ElasticWorkflowEventSinkTests
{
    private const string ElasticUrl = "http://localhost:9200";

    [Fact]
    public async Task WriteAsync_Should_Index_WorkflowEvent_Successfully2()
    {
        // Arrange
        var indexPrefix = $"test-events-{Guid.NewGuid():N}";

        var actualIndexName = $"{indexPrefix}-{DateTime.UtcNow:yyyy.MM.dd}";

        var settings = new ElasticsearchClientSettings(new Uri(ElasticUrl));

        var client = new ElasticsearchClient(settings);

        var sink = new ElasticWorkflowEventSink(client,
            Options.Create(new ElasticOptions
            {
                Url = ElasticUrl,
                IndexPrefix = indexPrefix
            }),
            NullLogger<ElasticWorkflowEventSink>.Instance);

        var evt = new WorkflowEvent(Guid.NewGuid(), "Planning", "stage_started", "unit test event", DateTimeOffset.UtcNow, new Dictionary<string, object>
        {
            ["Symbol"] = "AAPL"
        });

        // Act
        await sink.WriteAsync(evt, CancellationToken.None);

        await client.Indices.RefreshAsync(actualIndexName);

        var response = await client.SearchAsync<WorkflowEventDocument>(s => s
        .Indices(actualIndexName)
        .Query(q => q.Term(t => t.Field(f => f.EventType).Value("stage_started"))));

        // Assert
        response.IsValidResponse.Should().BeTrue( because: response.DebugInformation);
        response.Documents.Should().NotBeEmpty();

        var document = response.Documents.First();

        document.Stage.Should().Be("Planning");
        document.EventType.Should().Be("stage_started");
        document.Message.Should().Be("unit test event");
    }
}