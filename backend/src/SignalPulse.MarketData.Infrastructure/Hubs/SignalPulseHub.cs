using Microsoft.AspNetCore.SignalR;

namespace SignalPulse.MarketData.Infrastructure.Hubs;

public class SignalPulseHub : Hub
{
    public async Task JoinTenantGroup(Guid tenantId) => await Groups.AddToGroupAsync(Context.ConnectionId, TenantGroup(tenantId));
    public async Task LeaveTenantGroup(Guid tenantId) => await Groups.RemoveFromGroupAsync(Context.ConnectionId, TenantGroup(tenantId));
    private static string TenantGroup(Guid tenantId) => tenantId.ToString();
}