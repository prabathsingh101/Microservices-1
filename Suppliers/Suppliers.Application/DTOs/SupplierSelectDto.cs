using System;

namespace Suppliers.Application.DTOs
{
    public class SupplierSelectDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? GstIn { get; set; }
        public string? Phone { get; set; }
    }
}
