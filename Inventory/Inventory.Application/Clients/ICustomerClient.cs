using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Inventory.Application.Clients
{
    public interface ICustomerClient
    {
        Task<Dictionary<Guid, string>> GetCustomerNamesAsync(List<Guid> customerIds);
        Task<List<CustomerLookupDto>> GetCustomersForLookupAsync();
        Task<List<Guid>> SearchCustomerIdsByNameAsync(string searchName);
        Task RecordSaleAsync(Guid customerId, decimal amount, string referenceId, string description, string createdBy, string? branchId, Guid? companyId);
        Task RecordReceiptAsync(Guid? customerId, decimal amount, string paymentMode, string referenceNumber, string remarks, string createdBy, string? branchId, Guid? companyId);
        Task<CustomerLookupDto?> GetCustomerByIdAsync(Guid id);
    }
}
