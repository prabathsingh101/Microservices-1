using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Clients
{
    public interface ICustomerClient
    {
        // Batch call method jo IDs lekar Name ka Dictionary (Map) dega [cite: 2026-02-05]
        Task<Dictionary<Guid, string>> GetCustomerNamesAsync(List<Guid> customerIds);

        Task<List<CustomerLookupDto>> GetCustomersForLookupAsync();

        Task<List<Guid>> SearchCustomerIdsByNameAsync(string searchName);

        Task RecordSaleAsync(Guid customerId, decimal amount, string referenceId, string description, string createdBy, Guid? branchId, Guid? companyId);
        Task<CustomerLookupDto?> GetCustomerByIdAsync(Guid id);
    }
}
