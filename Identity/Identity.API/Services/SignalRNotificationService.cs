using Identity.API.Hubs;
using Identity.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Identity.API.Services;

public class SignalRNotificationService : ISignalRNotificationService
{
    private readonly IHubContext<AuthHub> _hubContext;
    
    public SignalRNotificationService(IHubContext<AuthHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task SendForceLogoutAsync(string userId, CancellationToken ct)
    {
        return _hubContext.Clients.User(userId).SendAsync("ReceiveForceLogout", ct);
    }
}
