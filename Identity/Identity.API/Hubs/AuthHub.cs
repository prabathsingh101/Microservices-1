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
    
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _connectedUsers = new();

    public override Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            _connectedUsers[Context.ConnectionId] = userId;
        }
        Console.WriteLine($"[AuthHub] User connected: {userId} with ConnectionId: {Context.ConnectionId}");
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _connectedUsers.TryRemove(Context.ConnectionId, out _);
        Console.WriteLine($"[AuthHub] User disconnected: {Context.UserIdentifier} with ConnectionId: {Context.ConnectionId}");
        return base.OnDisconnectedAsync(exception);
    }

    public static List<Guid> GetOnlineUserIds()
    {
        var guids = new List<Guid>();
        foreach (var userId in _connectedUsers.Values.Distinct())
        {
            if (Guid.TryParse(userId, out var guid))
            {
                guids.Add(guid);
            }
        }
        return guids;
    }
}
