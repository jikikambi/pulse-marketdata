using FakeItEasy;
using Microsoft.AspNetCore.SignalR;
using SignalPulse.MarketData.Infrastructure.Hubs;
using SignalPulse.MarketData.Infrastructure.Messaging;
using SignalPulse.MarketData.Infrastructure.RedisStore;

namespace SignalPulse.MarketData.Infrastructure.UnitTests.Messaging
{
    public class SignalRDomainEventPublisherTests
    {
        private readonly IHubContext<SignalPulseHub> _hubContext;
        private readonly IEventSequenceStore _sequenceStore;
        private readonly SignalRDomainEventPublisher _publisher;
        private readonly IHubClients _hubClients;
        private readonly IClientProxy _allClientsProxy;
        private readonly IClientProxy _groupClientsProxy;

        public SignalRDomainEventPublisherTests()
        {
            _hubContext = A.Fake<IHubContext<SignalPulseHub>>();
            _sequenceStore = A.Fake<IEventSequenceStore>();
            _hubClients = A.Fake<IHubClients>();
            _allClientsProxy = A.Fake<IClientProxy>();
            _groupClientsProxy = A.Fake<IClientProxy>();

            A.CallTo(() => _hubContext.Clients).Returns(_hubClients);
            A.CallTo(() => _hubClients.All).Returns(_allClientsProxy);

            _publisher = new SignalRDomainEventPublisher(_hubContext, _sequenceStore);
        }

        [Fact]
        public async Task PublishAsync_WithClientId_ShouldSendToGroupAndAll()
        {
            // Arrange
            var eventType = "TestEvent";
            var eventId = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            var timestamp = DateTimeOffset.UtcNow;
            var payload = new { ClientId = clientId, Data = "Sample" };

            var seq = 123L;
            A.CallTo(() => _sequenceStore.GetNextAsync(A<CancellationToken>._))
                .Returns(seq);

            A.CallTo(() => _hubClients.Group(clientId.ToString()))
                .Returns(_groupClientsProxy);

            // Act
            await _publisher.PublishAsync(eventType, eventId, payload, timestamp);

            // Assert
            A.CallTo(() =>
                _groupClientsProxy.SendCoreAsync(eventType, A<object[]>.That.Matches(args => args.Length == 1 && args[0] is SignalREvent), A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() =>
                _allClientsProxy.SendCoreAsync(eventType, A<object[]>.That.Matches(args => args.Length == 1 && args[0] is SignalREvent), A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task PublishAsync_WithoutClientId_ShouldSendOnlyToAll()
        {
            // Arrange
            var eventType = "TestEvent";
            var eventId = Guid.NewGuid();
            var timestamp = DateTimeOffset.UtcNow;
            var payload = new { Message = "No client id" };

            var seq = 321L;

            A.CallTo(() => _sequenceStore.GetNextAsync(A<CancellationToken>._))
                .Returns(seq);

            // Act
            await _publisher.PublishAsync(eventType, eventId, payload, timestamp);

            // Assert
            A.CallTo(() => _hubClients.Group(A<string>._))
                .MustNotHaveHappened();

            A.CallTo(() => _allClientsProxy.SendCoreAsync(eventType, A<object[]>.That.Matches(args => args.Length == 1 && args[0] is SignalREvent), A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }
    }
}