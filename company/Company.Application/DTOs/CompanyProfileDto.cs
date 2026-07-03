namespace Company.Application.DTOs
{
    // Response ke liye use hoga (Read Operations)
    public record CompanyProfileDto(
        Guid Id,
        string? CompanyCode,
        string? CompanyType,
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
        string? PurchaseOrderCreationMessage,
        string? PurchaseOrderStatusUpdateMessage,
        string? SaleOrderCreationMessage,
        string? SaleOrderConfirmationMessage,
        string? RazorpayKeyId,
        string? RazorpaySecretKey,
        string? RazorpayXAccountNumber,
        List<AddressDto> Addresses, 
        BankDetailDto BankInfo,
        List<AuthorizedSignatoryDto> AuthorizedSignatories
    );

    // Shared Records
    public record AddressDto(
        object? Id = null, 
        string BranchName = "Head Office",
        string AddressLine1 = "", 
        string AddressLine2 = "", 
        string City = "", 
        string State = "", 
        string StateCode = "", 
        string PinCode = "", 
        string Country = "India",
        string? Email = null,
        string? Phone = null,
        string? ContactPerson = null,
        string? Gstin = null,
        bool IsHeadOffice = false,
        Guid? CompanyProfileId = null
    );

    public record BankDetailDto(
        object? Id = null, 
        string BankName = "", 
        string BranchName = "", 
        string AccountNumber = "", 
        string IfscCode = "", 
        string AccountType = "Current",
        string? Email = null,
        string? UpiId = null
    );

    public record AuthorizedSignatoryDto(
        object? Id = null, 
        string PersonName = "", 
        string Designation = "", 
        string? SignatureImageUrl = null, 
        string? Email = null,
        bool IsDefault = false
    );

    // Request ke liye use hoga (Create/Update)
    public record UpsertCompanyRequest(
        Guid? CompanyId,
        string? CompanyCode,
        string? CompanyType,
        bool? IsInternalSync, // 🚀 FIX: Flag to prevent automatic subscription creation on sync
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
        bool? SmtpUseSsl,
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
        string? PurchaseOrderCreationMessage,
        string? PurchaseOrderStatusUpdateMessage,
        string? SaleOrderCreationMessage,
        string? SaleOrderConfirmationMessage,
        string? RazorpayKeyId,
        string? RazorpaySecretKey,
        string? RazorpayXAccountNumber,
        List<AddressDto> Addresses,
        BankDetailDto BankInfo,
        List<AuthorizedSignatoryDto> AuthorizedSignatories
    );
}
