using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Company.Domain.Entities
{
    public class CompanyProfile
    {
        public Guid Id { get; set; }
        public string? CompanyCode { get; set; }
        public string? CompanyType { get; set; }
        public string? Name { get; set; }
        public string? Tagline { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? Gstin { get; set; }
        public string? LogoUrl { get; set; }
        public string? PrimaryEmail { get; set; }
        public string? Email { get; set; }
        public string? SmtpEmail { get; set; }
        public string? SmtpPassword { get; set; }
        public string? SmtpHost { get; set; }
        public int? SmtpPort { get; set; }
        public bool SmtpUseSsl { get; set; } = true;
        public string? PrimaryPhone { get; set; }
        public string? Website { get; set; }
        public string? Message { get; set; }
        public string? DriverWhatsAppMessage { get; set; }
        public int SaleReturnWindowValue { get; set; } = 72;
        public string? SaleReturnWindowUnit { get; set; } = "Hours";
        public string? SaleReturnPolicyDisclaimer { get; set; }

        public int PurchaseReturnWindowValue { get; set; } = 72;
        public string PurchaseReturnWindowUnit { get; set; } = "Hours";
        public string? PurchaseReturnPolicyDisclaimer { get; set; }
        public bool IsActive { get; set; } = true;

        // Print Footer Messages [cite: 2026-04-08]
        public string? InvoiceFooterMessage { get; set; }
        public string? EstimateFooterMessage { get; set; }
        public string? PurchaseOrderFooterMessage { get; set; }
        public string? SaleOrderFooterMessage { get; set; }
        
        // Messaging Templates
        public string? PurchaseOrderCreationMessage { get; set; }
        public string? PurchaseOrderStatusUpdateMessage { get; set; }
        public string? SaleOrderCreationMessage { get; set; }
        public string? SaleOrderConfirmationMessage { get; set; }

        // Payment Gateway Settings
        public string? RazorpayKeyId { get; set; }
        public string? RazorpaySecretKey { get; set; }
        public string? RazorpayXAccountNumber { get; set; }

        // Navigation Properties
        public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
        public virtual ICollection<BankDetail> BankDetails { get; set; } = new List<BankDetail>();

        // Authorized Signatories
        public virtual ICollection<AuthorizedSignatory> AuthorizedSignatories { get; set; } = new List<AuthorizedSignatory>();
    }
}

