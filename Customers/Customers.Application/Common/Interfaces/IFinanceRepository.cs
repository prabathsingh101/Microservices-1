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
        Task<CustomerLedger?> GetLastLedgerEntryAsync(Guid? customerId);
        Task AddLedgerEntryAsync(CustomerLedger ledgerEntry);
        Task SaveChangesAsync();

        Task<CustomerLedgerPagedResultDto> GetLedgerAsync(CustomerLedgerRequestDto request);
        Task<OutstandingPagedResultDto> GetOutstandingAsync(OutstandingRequestDto request);
        Task<decimal> GetTotalReceiptsAsync(DateRangeDto dateRange);
        Task<AdjustmentsSummaryDto> GetTotalAdjustmentsAsync(DateRangeDto dateRange);
        Task<decimal> GetTotalOutstandingAsync(string? branchId = null, string? companyId = null);
        Task<List<OutstandingDto>> GetPendingDuesAsync(string? branchId = null, string? companyId = null);
        Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(int months, string? branchId = null, string? companyId = null);
        Task<bool> IsReferenceUniqueAsync(string referenceNumber);
        Task<(bool IsUnique, string Source)> IsReferenceUniqueWithSourceAsync(string referenceNumber);
        Task<PaginatedListDto<ReceiptReportDto>> GetReceiptsReportAsync(ReceiptReportRequestDto request);
    }
}
