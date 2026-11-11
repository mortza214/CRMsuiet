using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CrmSuite.Api.Realtime;

[Authorize]
public sealed class NotificationHub : Hub
{
    public Task Ping() => Clients.Caller.SendAsync("pong", DateTime.UtcNow);
}
