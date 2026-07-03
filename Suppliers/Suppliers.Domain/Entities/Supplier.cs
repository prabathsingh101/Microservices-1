using Suppliers.Domain.Common;
using System;

namespace Suppliers.Domain.Entities
{
    public class Supplier : BaseAuditableEntity
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string Name { get; private set; }
        public string Phone { get; private set; }
        public string? GstIn { get; private set; }
        public string? Address { get; private set; }
        public string? Email { get; private set; }
        public string? DrugLicenseNo { get; private set; }
        public string? SupplierType { get; private set; }
        public string? FssaiLicenseNo { get; private set; }
        public string? AgriLicenseNo { get; private set; }
        
        // --- GST Compliance Columns ---
        public string? GstFilingFrequency { get; private set; }
        public decimal? GstComplianceScore { get; private set; }
        public string? GstFilingStatus { get; private set; }
        public string? LastFiledMonth { get; private set; }
        public DateTime? LastFilingDate { get; private set; }

        public Guid? DefaultPriceListId { get; private set; }

        public bool IsActive { get; private set; } = true;

        // --- Bank Details Columns ---
        public string? BankAccountNumber { get; private set; }
        public string? BankIfscCode { get; private set; }
        public string? BankAccountName { get; private set; }
        public string? BankName { get; private set; }
        public string? BankBranchName { get; private set; }
        public string? BankAddress { get; private set; }

        public void UpdateGstCompliance(string? frequency, decimal? score, string? status, string? month, DateTime? filingDate)
        {
            GstFilingFrequency = frequency;
            GstComplianceScore = score;
            GstFilingStatus = status;
            LastFiledMonth = month;
            LastFilingDate = filingDate;
            ModifiedOn = DateTime.Now;
        }

        private Supplier() { Name = null!; Phone = null!; }

        public Supplier(
            string name,
            string phone,
            string? gstin,
            string? address,
            string? email,
            string? createdBy,
            bool isActive,
            Guid? companyId,
            string? branchId,
            Guid? defaultPriceListId = null,
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
            )
        {
            Name = name;
            Phone = phone;
            GstIn = gstin;
            Address = address;
            Email = email;
            CreatedBy = createdBy;
            IsActive = isActive;
            CompanyId = companyId;
            BranchId = branchId;
            DefaultPriceListId = defaultPriceListId;
            DrugLicenseNo = drugLicenseNo;
            SupplierType = supplierType;
            FssaiLicenseNo = fssaiLicenseNo;
            AgriLicenseNo = agriLicenseNo;
            BankAccountNumber = bankAccountNumber;
            BankIfscCode = bankIfscCode;
            BankAccountName = bankAccountName;
            BankName = bankName;
            BankBranchName = bankBranchName;
            BankAddress = bankAddress;
            CreatedOn = DateTime.Now;
        }

        public void SetDefaultPriceList(Guid? priceListId)
        {
            if (priceListId == Guid.Empty) throw new ArgumentException("Invalid Price List ID");

            DefaultPriceListId = priceListId;
            ModifiedOn = DateTime.UtcNow;
        }

        public void UpdateDetails(
            string name, 
            string phone, 
            string? gstIn, 
            string? address, 
            string? email, 
            bool isActive, 
            Guid? defaultPriceListId, 
            Guid? companyId, 
            string? branchId, 
            string? modifiedBy, 
            string? drugLicenseNo,
            string? supplierType,
            string? fssaiLicenseNo,
            string? agriLicenseNo,
            string? bankAccountNumber,
            string? bankIfscCode,
            string? bankAccountName,
            string? bankName,
            string? bankBranchName,
            string? bankAddress)
        {
            Name = name;
            Phone = phone;
            GstIn = gstIn;
            Address = address;
            Email = email;
            IsActive = isActive;
            CompanyId = companyId;
            BranchId = branchId;
            ModifiedBy = modifiedBy;
            ModifiedOn = DateTime.Now;
            DrugLicenseNo = drugLicenseNo;
            SupplierType = supplierType;
            FssaiLicenseNo = fssaiLicenseNo;
            AgriLicenseNo = agriLicenseNo;
            BankAccountNumber = bankAccountNumber;
            BankIfscCode = bankIfscCode;
            BankAccountName = bankAccountName;
            BankName = bankName;
            BankBranchName = bankBranchName;
            BankAddress = bankAddress;
            
            if (defaultPriceListId.HasValue && defaultPriceListId == Guid.Empty)
                 throw new ArgumentException("Invalid Price List ID");

            DefaultPriceListId = defaultPriceListId;
        }

        public void Deactivate() => IsActive = false;
    }
}