using Customers.Application.DTOs;
using Customers.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Customers.Application.Common.Interfaces
{
    public interface IFinanceRepository
    {
        Task AddReceiptAsync(CustomerReceipt receipt);
        Task<CustomerLedger?> GetLastLedgerEntryAsync(Guid customerId);
        Task AddLedgerEntryAsync(CustomerLedger ledgerEntry);
        Task SaveChangesAsync();

        Task<CustomerLedgerPagedResultDto> GetLedgerAsync(CustomerLedgerRequestDto request);
        Task<OutstandingPagedResultDto> GetOutstandingAsync(OutstandingRequestDto request);
        Task<decimal> GetTotalReceiptsAsync(DateRangeDto dateRange);
        Task<decimal> GetTotalOutstandingAsync();
        Task<List<OutstandingDto>> GetPendingDuesAsync();
        Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(int months);
        Task<bool> IsReferenceUniqueAsync(string referenceNumber);
        Task<PaginatedListDto<ReceiptReportDto>> GetReceiptsReportAsync(ReceiptReportRequestDto request);
    }
}
