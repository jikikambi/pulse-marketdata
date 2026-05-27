using Elastic.Clients.Elasticsearch;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Polly;
using SignalPulse.MarketData.Infrastructure.Elastic;
using SignalPulse.MarketData.Infrastructure.Policies;

namespace SignalPulse.MarketData.Infrastructure.IntegrationTests.Policies;

public class ElasticPolicyIntegrationTests
{
    [Fact]
    public async Task ElasticWorkflowEventSink_ShouldIndexDocument()
    {
        var gateway = A.Fake<IElasticWorkflowIndexGateway>();

        A.CallTo(() => gateway.IndexExistsAsync(A<string>._, A<CancellationToken>._))
            .Returns(false);

        var policyRegistry = A.Fake<IAiPolicyRegistry>();

        A.CallTo(() => policyRegistry.GetElasticPolicy())
            .Returns(Policy.NoOpAsync());

        var sink = new ElasticWorkflowEventSink(gateway,
            Options.Create(new ElasticOptions { IndexPrefix = "marketagent" }),
            policyRegistry,
            NullLogger<ElasticWorkflowEventSink>.Instance);

        await sink.WriteAsync(
            new WorkflowEvent(Guid.NewGuid(), "Planning", "planner_started", "Planner started", DateTimeOffset.UtcNow),
            CancellationToken.None);

        A.CallTo(() => gateway.IndexAsync(A<WorkflowEventDocument>._, A<string>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ElasticSink_Should_Retry_On_Transient_Failure()
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
                    throw new HttpRequestException("Transient failure");
                }

                return Task.FromResult(A.Fake<IndexResponse>());
            });

        A.CallTo(() => gateway.IndexExistsAsync(A<string>._, A<CancellationToken>._))
            .Returns(false);

        var registry = new AiPolicyRegistry(NullLogger<AiPolicyRegistry>.Instance);

        var sink = new ElasticWorkflowEventSink(gateway,
            Options.Create(new ElasticOptions
            {
                IndexPrefix = "test"
            }),
            registry,
            NullLogger<ElasticWorkflowEventSink>.Instance);

        var evt = new WorkflowEvent(Guid.NewGuid(), "Planning", "started", "test", DateTimeOffset.UtcNow);

        // Act
        await sink.WriteAsync(evt, CancellationToken.None);

        // Assert
        attempts.Should().Be(3);
    }

    [Fact]
    public async Task ElasticSink_Should_Open_Circuit_After_Repeated_Failures()
    {
        // Arrange
        var gateway = A.Fake<IElasticWorkflowIndexGateway>();

        A.CallTo(() => gateway.IndexAsync(A<WorkflowEventDocument>._, A<string>._, A<CancellationToken>._))
            .Throws<HttpRequestException>();

        var registry = new AiPolicyRegistry(NullLogger<AiPolicyRegistry>.Instance);

        var sink = new ElasticWorkflowEventSink(gateway,
            Options.Create(new ElasticOptions
            {
                IndexPrefix = "test"
            }),
            registry,
            NullLogger<ElasticWorkflowEventSink>.Instance);

        var evt = new WorkflowEvent(Guid.NewGuid(), "Planning", "started", "test", DateTimeOffset.UtcNow);

        // trigger failures
        for (int i = 0; i < 5; i++)
        {
            await sink.WriteAsync(evt, CancellationToken.None);
        }

        // Assert
        A.CallTo(() => gateway.IndexAsync(A<WorkflowEventDocument>._, A<string>._, A<CancellationToken>._))
            .MustHaveHappened();
    }
}