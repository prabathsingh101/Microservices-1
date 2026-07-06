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
        Task<bool> HasRefundOrAdjustmentAgainstReferenceAsync(Guid customerId, string referenceNumber);
        Task<bool> DeleteReceiptAsync(Guid id);
        Task<bool> ChequeNumberExistsAsync(string chequeNumber, string bankName);

        // --- NEW FEATURES ---
        Task<List<DebtorsAgeingDto>> GetDebtorsAgeingAsync(string? branchId = null);
        Task RecordPaymentReminderAsync(PaymentReminderLog log);
        Task<List<PaymentReminderLogDto>> GetPaymentReminderLogsAsync(Guid? customerId = null, string? branchId = null);
        Task RecordContraEntryAsync(ContraEntry contra);
        Task<List<ContraEntryDto>> GetContraEntriesAsync(string? branchId = null);
        Task UploadBankStatementAsync(BankStatement statement, List<BankStatementLine> lines);
        Task<List<BankStatementDto>> GetBankStatementsAsync(string? branchId = null);
        Task<List<BankStatementLineDto>> GetBankStatementLinesAsync(Guid statementId);
        Task<List<ReceiptReportDto>> GetUnmatchedSystemTransactionsAsync(string transactionType, string? branchId = null);
        Task<bool> ReconcileTransactionAsync(Guid lineId, string matchedTransactionType, Guid matchedTransactionId);
    }
}
