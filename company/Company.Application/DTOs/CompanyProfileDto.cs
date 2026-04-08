namespace Company.Application.DTOs
{
    // Response ke liye use hoga (Read Operations)
    public record CompanyProfileDto(
        int Id,
        string Name,
        string? Tagline,
        string RegistrationNumber,
        string Gstin,
        string? LogoUrl,
        string? PrimaryEmail,
        string? Email,
        string? SmtpEmail,
        string? SmtpPassword,
        string? SmtpHost,
        int? SmtpPort,
        bool SmtpUseSsl,
        string PrimaryPhone,
        string? Website,
        string? Message,
        string? DriverWhatsAppMessage,
        int SaleReturnWindowValue,
        string SaleReturnWindowUnit,
        string? SaleReturnPolicyDisclaimer,
        int PurchaseReturnWindowValue,
        string PurchaseReturnWindowUnit,
        string? PurchaseReturnPolicyDisclaimer,
        bool IsActive,
        string? InvoiceFooterMessage,
        string? EstimateFooterMessage,
        string? PurchaseOrderFooterMessage,
        string? SaleOrderFooterMessage,
        AddressDto Address, // Aapke request ke hisaab se name match
        BankDetailDto BankInfo,
        List<AuthorizedSignatoryDto> AuthorizedSignatories
    );

    // Shared Records
    public record AddressDto(
        int Id = 0, 
        string AddressLine1 = "", 
        string AddressLine2 = "", 
        string City = "", 
        string State = "", 
        string StateCode = "", 
        string PinCode = "", 
        string Country = "India",
        string? Email = null
    );

    public record BankDetailDto(
        int Id = 0, 
        string BankName = "", 
        string BranchName = "", 
        string AccountNumber = "", 
        string IfscCode = "", 
        string AccountType = "Current",
        string? Email = null
    );

    public record AuthorizedSignatoryDto(
        int Id = 0, 
        string PersonName = "", 
        string Designation = "", 
        string? SignatureImageUrl = null, 
        string? Email = null,
        bool IsDefault = false
    );

    // Request ke liye use hoga (Create/Update)
    public record UpsertCompanyRequest(
        string Name,
        string? Tagline,
        string RegistrationNumber,
        string Gstin,
        string? LogoUrl,
        string? PrimaryEmail,
        string? Email,
        string? SmtpEmail,
        string? SmtpPassword,
        string? SmtpHost,
        int? SmtpPort,
        bool SmtpUseSsl,
        string PrimaryPhone,
        string? Website,
        string? Message,
        string? DriverWhatsAppMessage,
        int SaleReturnWindowValue,
        string SaleReturnWindowUnit,
        string? SaleReturnPolicyDisclaimer,
        int PurchaseReturnWindowValue,
        string PurchaseReturnWindowUnit,
        string? PurchaseReturnPolicyDisclaimer,
        string? InvoiceFooterMessage,
        string? EstimateFooterMessage,
        string? PurchaseOrderFooterMessage,
        string? SaleOrderFooterMessage,
        AddressDto Address,
        BankDetailDto BankInfo,
        List<AuthorizedSignatoryDto> AuthorizedSignatories
    );
}
