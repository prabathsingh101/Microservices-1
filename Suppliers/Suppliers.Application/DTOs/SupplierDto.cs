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
    string? agriLicenseNo = null
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
    string? agriLicenseNo = null
);
