using System;
using System.Collections.Generic;
using System.Text;

namespace Company.Domain.Entities
{
    public class BankDetail
    {
        public int Id { get; set; }
        public string? BankName { get; set; }
        public string? BranchName { get; set; }
        public string? AccountNumber { get; set; }
        public string? IfscCode { get; set; }
        public string? AccountType { get; set; } = "Current"; // Savings/Current
        public string? Email { get; set; }
    }
}
