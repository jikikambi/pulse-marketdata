using FakeItEasy;
using Microsoft.AspNetCore.SignalR;
using SignalPulse.MarketData.Infrastructure.Hubs;

namespace SignalPulse.MarketData.Infrastructure.UnitTests.Hubs;

public class SignalPulseHubTests
{
    private readonly SignalPulseHub _hub;
    private readonly HubCallerContext _context = A.Fake<HubCallerContext>();
    private readonly IGroupManager _groups = A.Fake<IGroupManager>();

    public SignalPulseHubTests()
    {
        _hub = new SignalPulseHub
        {
            Context = _context,
            Groups = _groups
        };

        A.CallTo(() => _context.ConnectionId).Returns("conn-123");
    }

    [Fact]
    public async Task JoinTenantGroup_Should_Call_AddToGroupAsync()
    {
        var tenantId = Guid.NewGuid();

        await _hub.JoinTenantGroup(tenantId);

        A.CallTo(() => _groups.AddToGroupAsync("conn-123", tenantId.ToString(), default))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task LeaveTenantGroup_Should_Call_RemoveFromGroupAsync()
    {
        var tenantId = Guid.NewGuid();

        await _hub.LeaveTenantGroup(tenantId);

        A.CallTo(() => _groups.RemoveFromGroupAsync("conn-123", tenantId.ToString(), default))
            .MustHaveHappenedOnceExactly();
    }
}
