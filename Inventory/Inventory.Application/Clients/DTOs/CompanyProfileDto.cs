using System;
using System.Collections.Generic;

namespace Inventory.Application.Clients.DTOs
{
    public class CompanyProfileDto
    {
        public Guid Id { get; set; }
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
        public List<AddressDto> Addresses { get; set; } = new();
        public AddressDto Address => Addresses.Count > 0 ? Addresses[0] : new AddressDto();
        public BankDetailDto BankInfo { get; set; }
        public string? PurchaseOrderCreationMessage { get; set; }
        public string? PurchaseOrderStatusUpdateMessage { get; set; }
        public string? SaleOrderCreationMessage { get; set; }
        public string? SaleOrderConfirmationMessage { get; set; }
    }

    public class AddressDto
    {
        public int Id { get; set; }
        public string? BranchName { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string StateCode { get; set; }
        public string PinCode { get; set; }
        public string Country { get; set; }
        public string Email { get; set; }
        public string? Phone { get; set; }
        public string? ContactPerson { get; set; }
        public string? Gstin { get; set; }
        public bool IsHeadOffice { get; set; }
        public Guid? CompanyProfileId { get; set; }
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
        public string? UpiId { get; set; }
    }
}
