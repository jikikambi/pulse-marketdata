using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using FakeItEasy;
using FluentValidation;
using MarketData.Adapter.Api.Client;
using MarketData.Adapter.Api.Client.Services;
using MarketData.Adapter.Handler.Handlers;
using MarketData.Adapter.Shared.AlphaVantage.Request;
using MarketData.Adapter.Shared.AlphaVantage.Response;
using MarketData.Adapter.Shared.AlphaVantage.Services;
using MarketData.Adapter.Shared.Mappers;
using MarketData.Adapter.Shared.Options;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Refit;
using SignalPulse.Rdm.MarketData.AlphaVantage;
using SignalPulse.Shared.UnitTests.Logging;
using SignalPulse.Shared.UnitTests.Refit;

namespace MarketData.Adapter.Handler.UnitTests;

public class QuotePollingWorkerTests
{
    private readonly IFixture _fixture;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IServiceScope _scope;
    private readonly IServiceProvider _provider;
    private readonly IMarketDataAdapterClient _api;
    private readonly IAlphaVantageQuoteMapper _mapper;
    private readonly IAlphaVantageFallbackService _fallback;
    private readonly ILogger<QuotePollingWorker> _logger;
    private readonly IPublishEndpoint _publisher;
    private readonly ValidatedApiClient<AlphaVantageQuoteRequest, ApiResponse<AlphaVantageQuoteResponse>> _validatedClient;

    public QuotePollingWorkerTests()
    {
        _fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());

        _scopeFactory = A.Fake<IServiceScopeFactory>();
        _scope = A.Fake<IServiceScope>();
        _provider = A.Fake<IServiceProvider>();

        _api = A.Fake<IMarketDataAdapterClient>();
        _mapper = A.Fake<IAlphaVantageQuoteMapper>();
        _fallback = A.Fake<IAlphaVantageFallbackService>();
        _logger = A.Fake<ILogger<QuotePollingWorker>>();
        _publisher = A.Fake<IPublishEndpoint>();

        _validatedClient = A.Fake<ValidatedApiClient<AlphaVantageQuoteRequest, ApiResponse<AlphaVantageQuoteResponse>>>();

        A.CallTo(() => _scopeFactory.CreateScope()).Returns(_scope);
        A.CallTo(() => _scope.ServiceProvider).Returns(_provider);

        A.CallTo(() => _provider.GetService(typeof(ValidatedApiClient<AlphaVantageQuoteRequest, ApiResponse<AlphaVantageQuoteResponse>>)))
            .Returns(_validatedClient);

        A.CallTo(() => _provider.GetService(typeof(IPublishEndpoint)))
            .Returns(_publisher);
    }

    [Fact]
    public async Task PollOnce_ShouldPublishMessage_WhenQuoteIsValid()
    {
        // Arrange
        var quoteResponse = _fixture.Create<AlphaVantageQuoteResponse>();
        var apiResponse = RefitTestsHelper.CreateOkResponseMock(quoteResponse);

        var mappedMessage = _fixture.Create<AlphaVantageQuoteRdm>();

        A.CallTo(() => _fallback.TryGetOrFallbackAsync(A<AlphaVantageQuoteRequest>._, A<Func<Task<ApiResponse<AlphaVantageQuoteResponse>>>>._, A<CancellationToken>._, A<bool>._))
            .Returns(apiResponse);

        A.CallTo(() => _mapper.MapTo(apiResponse))
            .Returns(mappedMessage);

        var worker = CreateWorker();

        // Act
        await worker.PollOnce(CancellationToken.None);

        // Assert
        A.CallTo(() => _publisher.Publish(mappedMessage, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipPublishing_WhenMapperReturnsNull()
    {
        // Arrange
        var quoteResponse = _fixture.Create<AlphaVantageQuoteResponse>();
        var apiResponse = RefitTestsHelper.CreateOkResponseMock(quoteResponse);

        A.CallTo(() => _fallback.TryGetOrFallbackAsync(A<AlphaVantageQuoteRequest>._, A<Func<Task<ApiResponse<AlphaVantageQuoteResponse>>>>._, A<CancellationToken>._, A<bool>._))
            .Returns(apiResponse);

        A.CallTo(() => _mapper.MapTo(apiResponse)).Returns(null);

        var worker = CreateWorker();

        // Assert
        A.CallTo(() => _publisher.Publish(A<object>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLogWarning_WhenValidationFails()
    {
        // Arrange
        var validationException = new ValidationException("invalid");

        A.CallTo(() => _fallback.TryGetOrFallbackAsync(A<AlphaVantageQuoteRequest>._, A<Func<Task<ApiResponse<AlphaVantageQuoteResponse>>>>._, A<CancellationToken>._, A<bool>._))
            .Throws(validationException);

        var worker = CreateWorker();

        // Act
        await worker.PollOnce(CancellationToken.None);

        // Assert
        _logger.MustHaveLogged(LogLevel.Warning, validationException);
    }

    [Fact]
    public async Task PollOnce_ShouldLogWarning_WhenApiCallFails()
    {
        // Arrange
        var apiException = new HttpRequestException("API failure");

        A.CallTo(() => _fallback.TryGetOrFallbackAsync(A<AlphaVantageQuoteRequest>._, A<Func<Task<ApiResponse<AlphaVantageQuoteResponse>>>>._, A<CancellationToken>._, A<bool>._))
            .Throws(apiException);

        var worker = CreateWorker();

        // Act
        await worker.PollOnce(CancellationToken.None);

        // Assert
        _logger.MustHaveLogged(LogLevel.Warning, apiException);
    }

    private QuotePollingWorker CreateWorker()
    {
        var options = Options.Create(new AlphaVantageOptions
        {
            ApiKey = "demo",
            Symbols = ["MSFT"],
            UseLive = true
        });

        var polling = Options.Create(new PollingOptions
        {
            Interval = TimeSpan.FromMilliseconds(1)
        });

        return new QuotePollingWorker(_scopeFactory, _api, options, polling, _mapper, _logger, _fallback);
    }
}
