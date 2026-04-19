using Suppliers.Application.DTOs;
using Suppliers.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Suppliers.Application.Common.Interfaces
{
    public interface IFinanceRepository
    {
        Task AddPaymentAsync(SupplierPayment payment);
        Task<SupplierLedger?> GetLastLedgerEntryAsync(Guid supplierId);
        Task AddLedgerEntryAsync(SupplierLedger ledgerEntry);
        Task SaveChangesAsync();

        Task<SupplierLedgerPagedResultDto> GetLedgerAsync(SupplierLedgerRequestDto request);
        Task<List<PendingDueDto>> GetPendingDuesAsync();
        Task<decimal> GetTotalPaymentsAsync(DateRangeDto dateRange);
        Task<Dictionary<string, decimal>> GetGRNPaymentStatusesAsync(List<string> grnNumbers);
        Task<PaginatedListDto<PaymentReportDto>> GetPaymentsReportAsync(PaymentReportRequestDto request);
        Task<decimal> GetTotalPendingDuesAsync();
        Task<Dictionary<Guid, decimal>> GetSupplierBalancesAsync(List<Guid> supplierIds);
        Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(int months);
        Task<bool> ReferenceExistsAsync(string referenceNumber);
    }
}
