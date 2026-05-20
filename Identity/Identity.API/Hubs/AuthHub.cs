using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Identity.API.Hubs;

[Authorize]
public class AuthHub : Hub
{
    // Basic Hub for Auth-related real-time messaging.
    // Connections and disconnections are handled automatically.
    // The UserId provider maps the claims to Context.UserIdentifier automatically
    // when using standard JWT authentication.
    
    public override Task OnConnectedAsync()
    {
        Console.WriteLine($"[AuthHub] User connected: {Context.UserIdentifier} with ConnectionId: {Context.ConnectionId}");
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[AuthHub] User disconnected: {Context.UserIdentifier} with ConnectionId: {Context.ConnectionId}");
        return base.OnDisconnectedAsync(exception);
    }
}
