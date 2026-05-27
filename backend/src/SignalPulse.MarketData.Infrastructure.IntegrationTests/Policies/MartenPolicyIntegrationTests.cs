using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SignalPulse.MarketData.Infrastructure.Persistence;
using SignalPulse.MarketData.Infrastructure.Policies;

namespace SignalPulse.MarketData.Infrastructure.IntegrationTests.Policies;

public class MartenPolicyIntegrationTests
{
    [Fact]
    public async Task ResilientRepository_Should_Retry_Transient_Failures()
    {
        // Arrange
        var inner = new FailingQuoteRepository(failuresBeforeSuccess: 2);

        var registry = new AiPolicyRegistry(NullLogger<AiPolicyRegistry>.Instance);

        var repository = new ResilientQuoteReadRepository(inner, registry);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Should().NotBeEmpty();
    }
}