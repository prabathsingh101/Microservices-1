using System;
using System.Collections.Generic;
using System.Text;

namespace Company.Domain.Entities
{
    public class Address
    {
        public int Id { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? StateCode { get; set; }
        public string? PinCode { get; set; }
        public string? Country { get; set; } = "India";
        public string? Email { get; set; }
    }
}
