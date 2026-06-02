using System.Collections.Concurrent;
using System.Security.Claims;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private static readonly ConcurrentDictionary<string, string> _connectedEmails = new();
    private readonly IdentityDbContext _context;

    public ChatHub(IdentityDbContext context)
    {
        _context = context;
    }

    public override Task OnConnectedAsync()
    {
        var email = Context.User?.FindFirst(ClaimTypes.Email)?.Value 
                    ?? Context.User?.FindFirst("email")?.Value;
        if (!string.IsNullOrEmpty(email))
        {
            _connectedEmails[Context.ConnectionId] = email.ToLower();
            Console.WriteLine($"[ChatHub] Email registered: {email.ToLower()} with ConnectionId: {Context.ConnectionId}");
        }
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connectedEmails.TryRemove(Context.ConnectionId, out var email))
        {
            Console.WriteLine($"[ChatHub] Email disconnected: {email} with ConnectionId: {Context.ConnectionId}");
        }
        return base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string receiverEmail, string messageText, string? senderName = null, string? receiverName = null)
    {
        var senderEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value 
                          ?? Context.User?.FindFirst("email")?.Value;
        if (string.IsNullOrEmpty(senderEmail))
        {
            Console.WriteLine("[ChatHub] Sender email not found in claims.");
            return;
        }

        // 1. Save to Database
        var chatMessage = new ChatMessage(senderEmail, receiverEmail, messageText, senderName, receiverName);
        _context.ChatMessages.Add(chatMessage);
        await _context.SaveChangesAsync();

        // 2. Broadcast to Receiver (if online)
        var receiverConnectionIds = GetConnectionIdsForEmail(receiverEmail);
        if (receiverConnectionIds.Any())
        {
            await Clients.Clients(receiverConnectionIds).SendAsync("ReceiveMessage", chatMessage);
        }

        // 3. Broadcast to Sender's other open tabs/connections as well
        var senderConnectionIds = GetConnectionIdsForEmail(senderEmail);
        if (senderConnectionIds.Any())
        {
            await Clients.Clients(senderConnectionIds).SendAsync("ReceiveMessage", chatMessage);
        }
    }

    public static List<string> GetConnectionIdsForEmail(string email)
    {
        var emailLower = email.ToLower();
        return _connectedEmails
            .Where(kvp => kvp.Value == emailLower)
            .Select(kvp => kvp.Key)
            .ToList();
    }
}
