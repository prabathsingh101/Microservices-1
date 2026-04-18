namespace Identity.Application.DTOs
{
    public class AuthResponse
    {
        public Guid UserId { get; set; }
        public string AccessToken { get; init; } = null!;
        public string Email { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = null!;
        public DateTime ExpiresAt { get; init; }
        public List<string> Roles { get; init; } = new();
        public string? CompanyName { get; set; }
        public string? CompanyTagline { get; set; }
        public Guid? CompanyId { get; set; }
        public string SubscriptionStatus { get; set; } = "Active";
        public bool IsSubscriptionExpired { get; set; } = false;
        public IEnumerable<UserPermissionDto> Permissions { get; set; } = new List<UserPermissionDto>();
    }

    public class UserPermissionDto
    {
        public string MenuName { get; set; } = string.Empty;
        public string ActionCode { get; set; } = string.Empty; // e.g. "INVENTORY_VIEW"
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public string? AdditionalActions { get; set; } // e.g. "PRINT,APPROVE"
    }
}
