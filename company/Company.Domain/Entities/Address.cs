using System;
using System.Collections.Generic;
using System.Text;

namespace Company.Domain.Entities
{
    public class Address
    {
        public int Id { get; set; }
        public Guid? CompanyProfileId { get; set; }
        public string? BranchName { get; set; } // e.g. "Main Branch", "South Warehouse"
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? StateCode { get; set; }
        public string? PinCode { get; set; }
        public string? Country { get; set; } = "India";
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? ContactPerson { get; set; }
        public string? Gstin { get; set; } // Per-branch GSTIN if different from Head Office
        public bool IsHeadOffice { get; set; } = false;
    }
}
