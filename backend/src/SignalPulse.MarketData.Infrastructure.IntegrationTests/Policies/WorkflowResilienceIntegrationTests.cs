using Elastic.Clients.Elasticsearch;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SignalPulse.MarketData.Infrastructure.Elastic;
using SignalPulse.MarketData.Infrastructure.Policies;

namespace SignalPulse.MarketData.Infrastructure.IntegrationTests.Policies;

public class WorkflowResilienceIntegrationTests
{
    [Fact]
    public async Task Workflow_Should_Survive_Elastic_Transient_Failure()
    {
        // Arrange
        var gateway = A.Fake<IElasticWorkflowIndexGateway>();

        var attempts = 0;

        A.CallTo(() => gateway.IndexAsync(A<WorkflowEventDocument>._, A<string>._, A<CancellationToken>._))
            .ReturnsLazily(() =>
            {
                attempts++;

                if (attempts <= 2)
                {
                    throw new HttpRequestException("Elastic transient failure");
                }

                return Task.FromResult(A.Fake<IndexResponse>());
            });

        A.CallTo(() => gateway.IndexExistsAsync(A<string>._, A<CancellationToken>._))
            .Returns(false);

        var registry = new AiPolicyRegistry(NullLogger<AiPolicyRegistry>.Instance);

        var sink = new ElasticWorkflowEventSink(gateway,
            Options.Create(new ElasticOptions
            {
                IndexPrefix = "workflow"
            }),
            registry,
            NullLogger<ElasticWorkflowEventSink>.Instance);

        var evt = new WorkflowEvent(Guid.NewGuid(), "Planning", "workflow_started", "workflow test", DateTimeOffset.UtcNow);

        // Act
        await sink.WriteAsync(evt, CancellationToken.None);

        // Assert
        attempts.Should().Be(3);
    }
}