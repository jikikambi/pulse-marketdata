using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SignalPulse.MarketData.Application.AI;
using SignalPulse.MarketData.Application.Handlers;
using SignalPulse.MarketData.Application.Interfaces;
using SignalPulse.MarketData.Domain.Common;
using SignalPulse.MarketData.Domain.Exceptions;
using SignalPulse.MarketData.Domain.Quotes;
using SignalPulse.MarketData.Infrastructure.Messaging;
using SignalPulse.MarketData.Infrastructure.Persistence;
using SignalPulse.MarketData.Infrastructure.ReadModels;
using SignalPulse.MarketData.Infrastructure.RedisStore;

namespace SignalPulse.MarketData.Application.UnitTests.Handlers;

public class AlphaVantageQuoteHandlerTests
{
    private readonly IAggregateRepository _repo = A.Fake<IAggregateRepository>();
    private readonly IIdempotencyStore _idem = A.Fake<IIdempotencyStore>();
    private readonly IDomainEventPublisher _publisher = A.Fake<IDomainEventPublisher>();
    private readonly IAiInsightProvider<QuoteInsightInput> _ai = A.Fake<IAiInsightProvider<QuoteInsightInput>>();
    private readonly IReadModelRepository<QuoteInsightReadModel> _readRepo = A.Fake<IReadModelRepository<QuoteInsightReadModel>>();
    private readonly ILogger<AlphaVantageQuoteHandler> _logger = A.Fake<ILogger<AlphaVantageQuoteHandler>>();

    private readonly AlphaVantageQuoteHandler _sut;

    public AlphaVantageQuoteHandlerTests()
    {
        _sut = new AlphaVantageQuoteHandler(_repo, _idem, _publisher, _ai, _readRepo, _logger);
    }

    private void AllowExecution()
    {
        A.CallTo(() => _idem.TryMarkProcessedAsync(A<string>._, A<CancellationToken>._))
            .Returns(true);
    }

    [Fact]
    public async Task Handle_Should_ShortCircuit_When_Idempotent()
    {
        var rdm = MarketDataRdmBuilder.ValidQuote();

        A.CallTo(() => _idem.TryMarkProcessedAsync(A<string>._, A<CancellationToken>._))
            .Returns(false);

        await _sut.Handle(rdm, CancellationToken.None);

        A.CallTo(() => _repo.LoadAsync<QuoteAggregate>(A<Guid>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_Should_Create_New_Aggregate()
    {
        var rdm = MarketDataRdmBuilder.ValidQuote();

        AllowExecution();

        A.CallTo(() => _repo.LoadAsync<QuoteAggregate>(A<Guid>._, A<CancellationToken>._))
            .Returns((QuoteAggregate?)null);

        A.CallTo(() => _repo.PersistAsync(A<QuoteAggregate>._, A<CancellationToken>._))
            .Returns(new PersistResult(true, []));

        A.CallTo(() => _ai.GenerateAsync(A<QuoteInsightInput>._, A<CancellationToken>._))
            .Returns(new AIInsightResult("Bullish", "Up", "Low", "Test"));

        await _sut.Handle(rdm, CancellationToken.None);

        A.CallTo(() => _repo.PersistAsync(A<QuoteAggregate>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_Should_Update_Existing_Aggregate()
    {
        var rdm = MarketDataRdmBuilder.ValidQuote();

        var aggregate = QuoteAggregate.Create(rdm.Symbol, rdm.Price, rdm.ChangePercent);

        AllowExecution();

        A.CallTo(() => _repo.LoadAsync<QuoteAggregate>(A<Guid>._, A<CancellationToken>._))
            .Returns(aggregate);

        A.CallTo(() => _repo.PersistAsync(A<QuoteAggregate>._, A<CancellationToken>._))
            .Returns(new PersistResult(true, []));

        A.CallTo(() => _ai.GenerateAsync(A<QuoteInsightInput>._, A<CancellationToken>._))
            .Returns(new AIInsightResult("Bullish", "Up", "Low", "Strong earnings"));

        await _sut.Handle(rdm, CancellationToken.None);

        aggregate.Price.Should().Be(rdm.Price);
    }

    [Fact]
    public async Task Handle_Should_Save_ReadModel_And_Publish_Event()
    {
        var rdm = MarketDataRdmBuilder.ValidQuote();

        var insight = new AIInsightResult("Bullish", "Up", "Low", "Strong earnings");

        AllowExecution();

        A.CallTo(() => _repo.LoadAsync<QuoteAggregate>(A<Guid>._, A<CancellationToken>._))
            .Returns((QuoteAggregate?)null);

        A.CallTo(() => _repo.PersistAsync(A<QuoteAggregate>._, A<CancellationToken>._))
            .Returns(new PersistResult(true, []));

        A.CallTo(() => _ai.GenerateAsync(A<QuoteInsightInput>._, A<CancellationToken>._))
            .Returns(insight);

        await _sut.Handle(rdm, CancellationToken.None);

        A.CallTo(() => _readRepo.UpsertAsync(A<QuoteInsightReadModel>.That.Matches(x =>
                    x.Sentiment == "Bullish" && x.Symbol == rdm.Symbol), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _publisher.PublishAsync(A<string>._, A<Guid>._, A<object>._, A<DateTimeOffset>._, A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task Handle_Should_Throw_And_Log_When_Invalid_Data()
    {
        var rdm = MarketDataRdmBuilder.InvalidQuote();

        AllowExecution();

        A.CallTo(() => _repo.LoadAsync<QuoteAggregate>(A<Guid>._, A<CancellationToken>._))
            .Returns((QuoteAggregate?)null);

        Func<Task> act = () => _sut.Handle(rdm, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidQuoteException>();

        A.CallTo(_logger).Where(call => call.Method.Name.Contains("Log"))
            .MustHaveHappened();
    }
}