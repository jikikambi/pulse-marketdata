using FakeItEasy;
using FluentAssertions;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Models.Enums;
using SignalPulse.MarketData.Application.AI.Services.Agents;
using System.Globalization;

namespace SignalPulse.MarketAgent.IntegrationTests;

public class MarketAgentPrometheusTests
{
    [Fact]
    public async Task workflow_metrics_are_exposed()
    {
        await using var host = await MarketAgentTestHost.CreateAsync([new SlowStage()]);

        await host.Engine.RunAsync(CreateInput(), CancellationToken.None);

        var metrics = await host.Client.GetStringAsync("/metrics");

        metrics.Should().Contain("marketagent_workflow_started");

        metrics.Should().Contain("marketagent_workflow_completed");
    }

    [Fact]
    public async Task WorkflowFailed_metric_increments()
    {
        await using var host = await MarketAgentTestHost.CreateAsync([new ExplodingStage()], RecoveryStrategy.Terminate);

        await host.Engine.RunAsync(CreateInput(), CancellationToken.None);

        var metrics = await host.Client.GetStringAsync("/metrics");

        metrics.Should().Contain("marketagent_workflow_failed");
    }

    [Fact]
    public async Task RecoveryApplied_metric_increments()
    {
        await using var host = await MarketAgentTestHost.CreateAsync([new ExplodingStage()], RecoveryStrategy.Skip);

        await host.Engine.RunAsync(CreateInput(), CancellationToken.None);

        var metrics = await host.Client.GetStringAsync("/metrics");

        var recoveryLine = metrics.Split('\n', StringSplitOptions.RemoveEmptyEntries).First(x => x.StartsWith("marketagent_workflow_recoveries_total"));

        var value = double.Parse(recoveryLine.Split(' ').Last(), CultureInfo.InvariantCulture);

        value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task StageDuration_records_measurement()
    {
        await using var host = await MarketAgentTestHost.CreateAsync([new ExplodingStage()], RecoveryStrategy.Skip);

        await host.Engine.RunAsync(CreateInput(), CancellationToken.None);

        var metrics = await host.Client.GetStringAsync("/metrics");

        metrics.Should().Contain("marketagent_stage_duration");
    }

    [Fact]
    public async Task StageDuration_histogram_is_recorded()
    {
        await using var host = await MarketAgentTestHost.CreateAsync([new SlowStage()]);

        await host.Engine.RunAsync(CreateInput(), CancellationToken.None);

        var metrics = await host.Client.GetStringAsync("/metrics");

        metrics.Should().Contain("marketagent_stage_duration_milliseconds_bucket");
        metrics.Should().Contain("marketagent_stage_duration_milliseconds_sum");
        metrics.Should().Contain("marketagent_stage_duration_milliseconds_count");
    }

    [Fact]
    public async Task StageDuration_histogram_has_non_zero_count()
    {
        await using var host = await MarketAgentTestHost.CreateAsync([new SlowStage()]);

        await host.Engine.RunAsync(CreateInput(), CancellationToken.None);

        var metrics = await host.Client.GetStringAsync("/metrics");

        var countLine = metrics.Split('\n', StringSplitOptions.RemoveEmptyEntries).First(x => x.StartsWith("marketagent_stage_duration_milliseconds_count"));

        var count = double.Parse(countLine.Split(' ').Last(), CultureInfo.InvariantCulture);

        count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Reroute_emits_recovery_metric()
    {
        await using var host = await MarketAgentTestHost.CreateAsync([new ExplodingStage(), A.Fake<IMarketAgentStage>()], RecoveryStrategy.Reroute);

        await host.Engine.RunAsync(CreateInput(), CancellationToken.None);

        var metrics = await host.Client.GetStringAsync("/metrics");

        metrics.Should().Contain("strategy=\"Reroute\"");
    }

    [Fact]
    public async Task Reroute_increments_recovery_counter()
    {
        var riskStage = A.Fake<IMarketAgentStage>();

        A.CallTo(() => riskStage.Stage).Returns(MarketAgentStage.RiskEvaluation);

        A.CallTo(() => riskStage.DependsOn).Returns([MarketAgentStage.Reasoning]);

        A.CallTo(() => riskStage.ExecuteAsync(A<MarketAgentWorkflowContext>._, A<CancellationToken>._))
            .Returns(Task.CompletedTask);

        await using var host = await MarketAgentTestHost.CreateAsync([new ExplodingStage(), riskStage], RecoveryStrategy.Reroute, MarketAgentStage.RiskEvaluation);

        await host.Engine.RunAsync(CreateInput(), CancellationToken.None);

        var metrics = await host.Client.GetStringAsync("/metrics");

        var line = metrics.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Single(x => x.StartsWith("marketagent_workflow_recoveries_total") && x.Contains("strategy=\"Reroute\""));

        line.Should().Contain("alternate_stage=\"RiskEvaluation\"");

        var value = double.Parse(line.Split(' ').Last(), CultureInfo.InvariantCulture);

        value.Should().BeGreaterThan(0);
    }

    private static QuoteInsightInput CreateInput() => new("MSFT", 100m, 2m, 1000, Guid.NewGuid());
}