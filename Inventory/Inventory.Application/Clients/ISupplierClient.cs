using Inventory.Application.PurchaseReturn;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Clients
{
    public interface ISupplierClient
    {
        Task<List<SupplierSelectDto>> GetSuppliersByIdsAsync(List<Guid> supplierIds);
        Task<bool> RecordPurchaseAsync(Guid supplierId, decimal amount, string referenceId, string description, string createdBy);
        Task<Dictionary<string, decimal>> GetGRNPaymentStatusesAsync(List<string> grnNumbers);
        Task<Dictionary<Guid, decimal>> GetSupplierBalancesAsync(List<Guid> supplierIds);
        Task<bool> RecordPurchaseReturnAsync(Guid supplierId, decimal amount, string referenceId, string description, string createdBy);
        Task<List<Guid>> SearchSupplierIdsByNameAsync(string name);
        Task<SupplierSelectDto?> GetSupplierByIdAsync(Guid id);
    }
}

