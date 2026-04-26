using System;

namespace Identity.Application.DTOs
{
    public class MenuDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public Guid? ParentId { get; set; }
        public int Order { get; set; }
        public Guid? CompanyId { get; set; }
        public string? BranchId { get; set; }
        public List<MenuDto> Children { get; set; } = new List<MenuDto>();
    }
}
