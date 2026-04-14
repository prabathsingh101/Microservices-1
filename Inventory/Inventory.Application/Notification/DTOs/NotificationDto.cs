public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public string Type { get; set; }
    public bool IsRead { get; set; }
    public string CreatedAtFormatted { get; set; } // e.g., "2 mins ago"
    public string TargetUrl { get; set; }
}
