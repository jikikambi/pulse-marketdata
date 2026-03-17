using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SignalPulse.MarketData.Domain.Exceptions;
using SignalPulse.MarketData.Domain.Forex;
using SignalPulse.MarketData.Application.AI;
using SignalPulse.MarketData.Application.Handlers;
using SignalPulse.MarketData.Application.Interfaces;
using SignalPulse.MarketData.Domain.Common;
using SignalPulse.MarketData.Infrastructure.Messaging;
using SignalPulse.MarketData.Infrastructure.Persistence;
using SignalPulse.MarketData.Infrastructure.ReadModels;
using SignalPulse.MarketData.Infrastructure.RedisStore;

namespace SignalPulse.MarketData.Application.UnitTests.Handlers;

public class AlphaVantageForexHandlerTests
{
    private readonly IAggregateRepository _repo = A.Fake<IAggregateRepository>();
    private readonly IIdempotencyStore _idem = A.Fake<IIdempotencyStore>();
    private readonly IDomainEventPublisher _publisher = A.Fake<IDomainEventPublisher>();
    private readonly IAiInsightProvider<ForexInsightInput> _ai = A.Fake<IAiInsightProvider<ForexInsightInput>>();
    private readonly IReadModelRepository<ForexInsightReadModel> _readRepo = A.Fake<IReadModelRepository<ForexInsightReadModel>>();
    private readonly ILogger<AlphaVantageForexHandler> _logger = A.Fake<ILogger<AlphaVantageForexHandler>>();

    private readonly AlphaVantageForexHandler _sut;

    public AlphaVantageForexHandlerTests()
    {
        _sut = new AlphaVantageForexHandler(_repo, _idem, _publisher, _ai, _readRepo, _logger);
    }

    [Fact]
    public async Task Handle_Should_ShortCircuit_When_Idempotent()
    {
        var rdm = MarketDataRdmBuilder.ValidForex();

        A.CallTo(() => _idem.TryMarkProcessedAsync(A<string>._, A<CancellationToken>._))
            .Returns(false);

        await _sut.Handle(rdm, CancellationToken.None);

        A.CallTo(() => _repo.LoadAsync<ForexAggregate>(A<Guid>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_Should_Create_New_Aggregate()
    {
        var rdm = MarketDataRdmBuilder.ValidForex();

        A.CallTo(() => _idem.TryMarkProcessedAsync(A<string>._, A<CancellationToken>._))
            .Returns(true);

        A.CallTo(() => _repo.LoadAsync<ForexAggregate>(A<Guid>._, A<CancellationToken>._))
            .Returns((ForexAggregate?)null);

        A.CallTo(() => _repo.PersistAsync(A<ForexAggregate>._, A<CancellationToken>._))
            .Returns(new PersistResult(true, []));

        A.CallTo(() => _ai.GenerateAsync(A<ForexInsightInput>._, A<CancellationToken>._))
            .Returns(new AIInsightResult("Bullish", "Up", "Low", "Test"));

        await _sut.Handle(rdm, CancellationToken.None);

        A.CallTo(() => _repo.PersistAsync(A<ForexAggregate>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_Should_Update_Existing_Aggregate()
    {
        var rdm = MarketDataRdmBuilder.ValidForex();

        var aggregate = ForexAggregate.Create(rdm.FromSymbol, rdm.ToSymbol, rdm.Open, rdm.High, rdm.Low, rdm.Close, rdm.ForexDate);

        A.CallTo(() => _idem.TryMarkProcessedAsync(A<string>._, A<CancellationToken>._))
            .Returns(true);

        A.CallTo(() => _repo.LoadAsync<ForexAggregate>(A<Guid>._, A<CancellationToken>._))
            .Returns(aggregate);

        A.CallTo(() => _repo.PersistAsync(A<ForexAggregate>._, A<CancellationToken>._))
            .Returns(new PersistResult(true, []));

        A.CallTo(() => _ai.GenerateAsync(A<ForexInsightInput>._, A<CancellationToken>._))
            .Returns(new AIInsightResult("Bullish", "Up", "Low", "Test"));

        await _sut.Handle(rdm, CancellationToken.None);

        aggregate.Close.Should().Be(rdm.Close);
    }

    [Fact]
    public async Task Handle_Should_Save_ReadModel_And_Publish_Event()
    {
        var rdm = MarketDataRdmBuilder.ValidForex();
        var insight = new AIInsightResult("Bullish", "Up", "Low", "Test");

        A.CallTo(() => _idem.TryMarkProcessedAsync(A<string>._, A<CancellationToken>._))
            .Returns(true);

        A.CallTo(() => _repo.LoadAsync<ForexAggregate>(A<Guid>._, A<CancellationToken>._))
            .Returns((ForexAggregate?)null);

        A.CallTo(() => _repo.PersistAsync(A<ForexAggregate>._, A<CancellationToken>._))
            .Returns(new PersistResult(true, []));

        A.CallTo(() => _ai.GenerateAsync(A<ForexInsightInput>._, A<CancellationToken>._))
            .Returns(insight);

        await _sut.Handle(rdm, CancellationToken.None);

        A.CallTo(() => _readRepo.UpsertAsync(A<ForexInsightReadModel>.That.Matches(x => x.Sentiment == "Bullish"), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _publisher.PublishAsync(A<string>._, A<Guid>._, A<object>._, A<DateTimeOffset>._, A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task Handle_Should_Throw_And_Log_When_Invalid_Data()
    {
        var rdm = MarketDataRdmBuilder.Invalid();

        A.CallTo(() => _idem.TryMarkProcessedAsync(A<string>._, A<CancellationToken>._))
            .Returns(true);

        A.CallTo(() => _repo.LoadAsync<ForexAggregate>(A<Guid>._, A<CancellationToken>._))
            .Returns((ForexAggregate?)null);

        Func<Task> act = () => _sut.Handle(rdm, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidForexException>();

        A.CallTo(_logger).Where(call => call.Method.Name == "Log" || call.Method.Name == "LogError")
            .MustHaveHappened();
    }
}