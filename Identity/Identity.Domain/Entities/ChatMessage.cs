using Identity.Domain.Common;

namespace Identity.Domain.Entities;

public class ChatMessage : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SenderEmail { get; set; } = null!;
    public string ReceiverEmail { get; set; } = null!;
    public string MessageText { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
    public string? SenderName { get; set; }
    public string? ReceiverName { get; set; }

    public ChatMessage() { }

    public ChatMessage(string senderEmail, string receiverEmail, string messageText, string? senderName = null, string? receiverName = null)
    {
        SenderEmail = senderEmail;
        ReceiverEmail = receiverEmail;
        MessageText = messageText;
        Timestamp = DateTime.UtcNow;
        IsRead = false;
        SenderName = senderName;
        ReceiverName = receiverName;
    }
}
