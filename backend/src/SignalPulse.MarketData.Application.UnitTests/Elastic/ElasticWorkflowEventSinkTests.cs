using Elastic.Clients.Elasticsearch;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Polly;
using SignalPulse.MarketData.Infrastructure.Elastic;
using SignalPulse.MarketData.Infrastructure.Policies;

namespace SignalPulse.MarketData.Application.UnitTests.Elastic;

public class ElasticWorkflowEventSinkTests
{
    [Fact]
    public async Task WriteAsync_Should_Index_WorkflowEvent_Successfully()
    {
        // Arrange
        var indexPrefix = $"test-events-{Guid.NewGuid():N}";
        var indexName = $"{indexPrefix}-{DateTime.UtcNow:yyyy.MM.dd}";

        var gateway = A.Fake<IElasticWorkflowIndexGateway>();

        A.CallTo(() => gateway.IndexExistsAsync(indexName, A<CancellationToken>._))
            .Returns(false);

        A.CallTo(() => gateway.IndexAsync(  A<WorkflowEventDocument>._, indexName, A<CancellationToken>._))
            .Returns(new IndexResponse()); 

        var policyRegistry = A.Fake<IAiPolicyRegistry>();

        A.CallTo(() => policyRegistry.GetElasticPolicy())
            .Returns(Policy.NoOpAsync());

        var sink = new ElasticWorkflowEventSink( gateway,
            Options.Create(new ElasticOptions { IndexPrefix = indexPrefix }),
            policyRegistry,
            NullLogger<ElasticWorkflowEventSink>.Instance);

        var evt = new WorkflowEvent( Guid.NewGuid(), 
            "Planning", "stage_started", "unit test event", 
            DateTimeOffset.UtcNow, 
            new Dictionary<string, object> { ["Symbol"] = "AAPL" });

        // Act
        await sink.WriteAsync(evt, CancellationToken.None);

        // Assert
        A.CallTo(() => gateway.IndexAsync( A<WorkflowEventDocument>._, indexName, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }
}