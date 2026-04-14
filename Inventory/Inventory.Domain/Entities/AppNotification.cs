using Inventory.Domain.Common;

public class AppNotification : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid(); // Primary Key
    public string Title { get; set; } // Alert Header (e.g., "Low Stock")
    public string Message { get; set; } // Detail Text
    public string Type { get; set; } // 'Inventory', 'PO', 'Security'
    public bool IsRead { get; set; } = false; // Default unread
    public DateTime CreatedAt { get; set; } = DateTime.Now; // Timestamp
    public string TargetUrl { get; set; } // Link to navigate (e.g., /app/inventory/stock)
}
