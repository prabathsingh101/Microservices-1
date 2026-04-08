using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Company.Domain.Entities
{
    public class CompanyProfile
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Aapki Company ka Naam
        public string? Tagline { get; set; } // Optional slogan
        public string RegistrationNumber { get; set; } = string.Empty; // PAN/VAT No.
        public string Gstin { get; set; } = string.Empty; // Tax ke liye sabse zaroori
        public string? LogoUrl { get; set; } // Report ke header ke liye optional
        public string? PrimaryEmail { get; set; }
        public string? Email { get; set; }
        public string? SmtpEmail { get; set; }
        public string? SmtpPassword { get; set; }
        public string? SmtpHost { get; set; }
        public int? SmtpPort { get; set; }
        public bool SmtpUseSsl { get; set; } = true;
        public string PrimaryPhone { get; set; } = string.Empty;
        public string? Website { get; set; }
        public string? Message { get; set; } // WhatsApp/SMS reminder message
        public string? DriverWhatsAppMessage { get; set; } // Custom message for driver tracking
        public int ReturnWindowValue { get; set; } = 72; // Default 72 hours
        public string ReturnWindowUnit { get; set; } = "Hours"; // "Hours", "Days", "Months"
        public bool IsActive { get; set; } = true;

        // Foreign Keys
        public int AddressId { get; set; }
        public virtual Address CompanyAddress { get; set; } = null!;

        public int BankDetailId { get; set; }
        public virtual BankDetail BankInformation { get; set; } = null!;

        // Authorized Signatories
        public virtual ICollection<AuthorizedSignatory> AuthorizedSignatories { get; set; } = new List<AuthorizedSignatory>();
    }
}

