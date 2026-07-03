using System;

namespace Suppliers.Application.DTOs;

public record SupplierDto(
    Guid id,
    string name,
    string phone,
    string? gstIn,
    string? address,
    string? email,
    bool? isActive,
    string? createdBy,
    Guid? defaultpricelistId,
    string? companyId,
    string? branchId,
    string? drugLicenseNo = null,
    string? supplierType = null,
    string? fssaiLicenseNo = null,
    string? agriLicenseNo = null,
    string? gstFilingFrequency = null,
    decimal? gstComplianceScore = null,
    string? gstFilingStatus = null,
    string? lastFiledMonth = null,
    DateTime? lastFilingDate = null,
    string? bankAccountNumber = null,
    string? bankIfscCode = null,
    string? bankAccountName = null,
    string? bankName = null,
    string? bankBranchName = null,
    string? bankAddress = null
);

public record CreateSupplierDto(
    string name,
    string phone,
    string? gstIn,
    string? address,
    string? email,
    string? createdBy,
    string? modifiedBy,
    Guid? defaultpricelistId,
    bool isActive,
    string? companyId,
    string? branchId,
    string? drugLicenseNo = null,
    string? supplierType = null,
    string? fssaiLicenseNo = null,
    string? agriLicenseNo = null,
    string? bankAccountNumber = null,
    string? bankIfscCode = null,
    string? bankAccountName = null,
    string? bankName = null,
    string? bankBranchName = null,
    string? bankAddress = null
);
