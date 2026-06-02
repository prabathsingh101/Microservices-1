using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Identity.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Identity.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ChatController : ControllerBase
{
    private readonly IdentityDbContext _context;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatController(IdentityDbContext context, IHubContext<ChatHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetChatHistory([FromQuery] string email1, [FromQuery] string email2)
    {
        if (string.IsNullOrEmpty(email1) || string.IsNullOrEmpty(email2))
        {
            return BadRequest("Both emails must be provided.");
        }

        var e1 = email1.ToLower();
        var e2 = email2.ToLower();

        var messages = await _context.ChatMessages
            .Where(m => (m.SenderEmail == e1 && m.ReceiverEmail == e2) || (m.SenderEmail == e2 && m.ReceiverEmail == e1))
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        return Ok(messages);
    }

    [HttpPost("read")]
    public async Task<IActionResult> MarkAsRead([FromQuery] string senderEmail, [FromQuery] string receiverEmail)
    {
        if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(receiverEmail))
        {
            return BadRequest("Both emails must be provided.");
        }

        var se = senderEmail.ToLower();
        var re = receiverEmail.ToLower();

        var unread = await _context.ChatMessages
            .Where(m => m.SenderEmail == se && m.ReceiverEmail == re && !m.IsRead)
            .ToListAsync();

        if (unread.Any())
        {
            foreach (var msg in unread)
            {
                msg.IsRead = true;
            }

            await _context.SaveChangesAsync();

            // Broadcast real-time read event to the sender (so their double check turns green)
            var senderConnectionIds = ChatHub.GetConnectionIdsForEmail(se);
            if (senderConnectionIds.Any())
            {
                await _hubContext.Clients.Clients(senderConnectionIds).SendAsync("MessagesRead", re);
            }
        }

        return NoContent();
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount([FromQuery] string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return BadRequest("Email must be provided.");
        }

        var count = await _context.ChatMessages
            .CountAsync(m => m.ReceiverEmail == email.ToLower() && !m.IsRead);

        return Ok(new { count });
    }

    [HttpGet("unread-summary")]
    public async Task<IActionResult> GetUnreadSummary([FromQuery] string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return BadRequest("Email must be provided.");
        }

        var summary = await _context.ChatMessages
            .Where(m => m.ReceiverEmail == email.ToLower() && !m.IsRead)
            .GroupBy(m => m.SenderEmail)
            .Select(g => new { SenderEmail = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(summary);
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file was uploaded.");
        }

        // Ensure wwwroot/uploads directory exists
        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        // Create a unique filename
        var extension = Path.GetExtension(file.FileName);
        var filename = $"{Guid.NewGuid()}{extension}";
        var filepath = Path.Combine(uploadsDir, filename);

        // Save file
        using (var stream = new FileStream(filepath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Return the relative URL under /api/identity/uploads/
        var fileUrl = $"/api/identity/uploads/{filename}";
        return Ok(new { url = fileUrl, fileName = file.FileName, fileSize = file.Length });
    }
}
