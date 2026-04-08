namespace Inventory.Application.Clients.DTOs
{
    public class CompanyProfileDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Tagline { get; set; }
        public string RegistrationNumber { get; set; }
        public string Gstin { get; set; }
        public string LogoUrl { get; set; }
        public string PrimaryEmail { get; set; }
        public string Email { get; set; }
        public string SmtpEmail { get; set; }
        public string SmtpPassword { get; set; }
        public string SmtpHost { get; set; }
        public int? SmtpPort { get; set; }
        public bool SmtpUseSsl { get; set; }
        public string PrimaryPhone { get; set; }
        public string Website { get; set; }
        public bool IsActive { get; set; }
        public int SaleReturnWindowValue { get; set; }
        public string SaleReturnWindowUnit { get; set; }
        public string? SaleReturnPolicyDisclaimer { get; set; }
        public int PurchaseReturnWindowValue { get; set; }
        public string PurchaseReturnWindowUnit { get; set; }
        public string? PurchaseReturnPolicyDisclaimer { get; set; }
        public AddressDto Address { get; set; }
        public BankDetailDto BankInfo { get; set; }
    }

    public class AddressDto
    {
        public int Id { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string StateCode { get; set; }
        public string PinCode { get; set; }
        public string Country { get; set; }
        public string Email { get; set; }
    }

    public class BankDetailDto
    {
        public int Id { get; set; }
        public string BankName { get; set; }
        public string BranchName { get; set; }
        public string AccountNumber { get; set; }
        public string IfscCode { get; set; }
        public string AccountType { get; set; }
        public string Email { get; set; }
    }
}
