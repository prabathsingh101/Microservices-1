using System;

namespace Customers.Application.DTOs
{
    public class CustomerLookupDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
    }
}