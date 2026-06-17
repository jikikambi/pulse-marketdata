using FakeItEasy;
using FluentAssertions;
using Microsoft.SemanticKernel;
using Polly;
using SignalPulse.MarketData.Application.AI;
using SignalPulse.MarketData.Application.AI.Models;
using SignalPulse.MarketData.Application.AI.Services.Agents;
using SignalPulse.MarketData.Application.AI.Services.Providers;
using SignalPulse.MarketData.Infrastructure.Policies;

namespace SignalPulse.MarketData.Application.UnitTests.AI;

public class SemanticKernelReasoningAgentTests
{
    [Fact]
    public async Task GenerateAsync_should_invoke_kernel()
    {
        var kernel = A.Fake<IKernelInvoker>();

        var policies = A.Fake<IAiPolicyRegistry>();

        A.CallTo(() => policies.GetReasonerPolicy())
            .Returns(Policy.NoOpAsync());

        A.CallTo(() => kernel.InvokeAsync(AgentConstants.ReasonerSkill, A<KernelArguments>._, A<CancellationToken>._))
            .Returns("""
            {             
                "sentiment":"bullish"
            }
            """);

        var sut = new SemanticKernelReasoningAgent(kernel, policies);

        var result = await sut.GenerateAsync(CreateInput(), null, CancellationToken.None);

        result.Should().Contain("bullish");
    }

    private static QuoteInsightInput CreateInput() => new("MSFT", 100m, 2m, 1000, Guid.NewGuid());
}