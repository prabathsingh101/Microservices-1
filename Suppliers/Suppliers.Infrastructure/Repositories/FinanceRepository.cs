using Suppliers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Suppliers.Application.DTOs;
using Suppliers.Application.Common.Interfaces;
using Suppliers.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Suppliers.Infrastructure.Repositories
{
    public class FinanceRepository(SupplierDbContext context, ICurrentUserService currentUserService) : IFinanceRepository
    {
        private readonly SupplierDbContext _context = context;
        private readonly ICurrentUserService _currentUserService = currentUserService;

        private Guid _companyId => _currentUserService.CompanyId ?? Guid.Empty;
        private string? _branchId => _currentUserService.BranchId == "All Branches" ? null : _currentUserService.BranchId;

        public async Task AddPaymentAsync(SupplierPayment payment)
        {
            payment.CompanyId = _companyId;
            if (string.IsNullOrEmpty(payment.BranchId))
            {
                payment.BranchId = _branchId;
            }
            await _context.SupplierPayments.AddAsync(payment);
        }

        public async Task<SupplierLedger?> GetLastLedgerEntryAsync(Guid supplierId)
        {
            return await _context.SupplierLedgers
                .Where(l => l.SupplierId == supplierId && l.CompanyId == _companyId && (l.BranchId == null || string.IsNullOrEmpty(_branchId) || l.BranchId == _branchId))
                .OrderByDescending(l => l.CreatedOn)
                .FirstOrDefaultAsync();
        }

        public async Task AddLedgerEntryAsync(SupplierLedger ledgerEntry)
        {
            if (ledgerEntry.CompanyId == Guid.Empty)
            {
                ledgerEntry.CompanyId = _companyId;
            }
            
            if (string.IsNullOrEmpty(ledgerEntry.BranchId))
            {
                ledgerEntry.BranchId = _branchId;
            }
            await _context.SupplierLedgers.AddAsync(ledgerEntry);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<SupplierLedgerPagedResultDto> GetLedgerAsync(SupplierLedgerRequestDto request)
        {
            var supplier = await _context.Suppliers.FirstOrDefaultAsync(x => x.Id == request.SupplierId && x.CompanyId == _companyId && (x.BranchId == null || string.IsNullOrEmpty(_branchId) || x.BranchId == _branchId));
            
            // 1. Fetch all entries for this supplier to compute correct chronological running balance
            var allLedgerEntries = await _context.SupplierLedgers
                .Where(l => l.SupplierId == request.SupplierId && l.CompanyId == _companyId && (l.BranchId == null || string.IsNullOrEmpty(_branchId) || l.BranchId == _branchId))
                .OrderBy(l => l.TransactionDate)
                .ThenBy(l => l.CreatedOn)
                .ToListAsync();

            decimal runningBalance = 0;
            foreach (var entry in allLedgerEntries)
            {
                runningBalance = runningBalance + entry.Credit - entry.Debit;
                entry.Balance = Math.Round(runningBalance, 2, MidpointRounding.AwayFromZero);
            }

            // 2. Current balance is the final computed balance
            var currentBalance = allLedgerEntries.Any() ? allLedgerEntries.Last().Balance : 0;

            // 3. Apply search and filtering on the corrected memory list
            var itemsQuery = allLedgerEntries.AsQueryable();

            if (request.StartDate.HasValue)
                itemsQuery = itemsQuery.Where(l => l.TransactionDate >= request.StartDate.Value);
            if (request.EndDate.HasValue)
                itemsQuery = itemsQuery.Where(l => l.TransactionDate <= request.EndDate.Value);

            if (!string.IsNullOrWhiteSpace(request.TypeFilter))
            {
                var type = request.TypeFilter.ToLower();
                itemsQuery = itemsQuery.Where(l => l.TransactionType.ToLower().Contains(type));
            }

            if (!string.IsNullOrWhiteSpace(request.ReferenceFilter))
            {
                var refId = request.ReferenceFilter.ToLower();
                itemsQuery = itemsQuery.Where(l => l.ReferenceId != null && l.ReferenceId.ToLower().Contains(refId));
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                itemsQuery = itemsQuery.Where(l => 
                    l.TransactionType.ToLower().Contains(term) || 
                    (l.ReferenceId != null && l.ReferenceId.ToLower().Contains(term)) || 
                    (l.Description != null && l.Description.ToLower().Contains(term))
                );
            }

            // Apply sorting
            itemsQuery = request.SortBy.ToLower() switch
            {
                "transactiondate" => request.SortOrder == "desc" ? itemsQuery.OrderByDescending(l => l.TransactionDate).ThenByDescending(l => l.CreatedOn) : itemsQuery.OrderBy(l => l.TransactionDate).ThenBy(l => l.CreatedOn),
                "transactiontype" => request.SortOrder == "desc" ? itemsQuery.OrderByDescending(l => l.TransactionType) : itemsQuery.OrderBy(l => l.TransactionType),
                "referenceid" => request.SortOrder == "desc" ? itemsQuery.OrderByDescending(l => l.ReferenceId) : itemsQuery.OrderBy(l => l.ReferenceId),
                "debit" => request.SortOrder == "desc" ? itemsQuery.OrderByDescending(l => l.Debit) : itemsQuery.OrderBy(l => l.Debit),
                "credit" => request.SortOrder == "desc" ? itemsQuery.OrderByDescending(l => l.Credit) : itemsQuery.OrderBy(l => l.Credit),
                "balance" => request.SortOrder == "desc" ? itemsQuery.OrderByDescending(l => l.Balance) : itemsQuery.OrderBy(l => l.Balance),
                _ => request.SortOrder == "desc" ? itemsQuery.OrderByDescending(l => l.TransactionDate).ThenByDescending(l => l.CreatedOn) : itemsQuery.OrderBy(l => l.TransactionDate).ThenBy(l => l.CreatedOn)
            };

            var totalCount = itemsQuery.Count();
            var items = itemsQuery
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new SupplierLedgerPagedResultDto
            {
                SupplierName = supplier?.Name ?? "Unknown",
                currentBalance = currentBalance,
                Ledger = new PaginatedListDto<SupplierLedger>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                }
            };
        }

        public async Task<List<PendingDueDto>> GetPendingDuesAsync(string? branchId = null, string? companyId = null)
        {
            var finalBranchId = !string.IsNullOrEmpty(branchId) ? branchId : _branchId;
            if (finalBranchId == "All Branches") finalBranchId = null;
            Guid finalCompanyId = _companyId;
            if (!string.IsNullOrEmpty(companyId) && Guid.TryParse(companyId, out var cg)) finalCompanyId = cg;

            var allLedgerEntries = await _context.SupplierLedgers
                .AsNoTracking()
                .Where(l => l.CompanyId == finalCompanyId && (string.IsNullOrEmpty(finalBranchId) || l.BranchId == finalBranchId))
                .ToListAsync();

            if (!allLedgerEntries.Any()) return new List<PendingDueDto>();

            var supplierBalances = allLedgerEntries
                .GroupBy(l => l.SupplierId)
                .Select(g => new
                {
                    SupplierId = g.Key,
                    Balance = g.Sum(l => l.Credit - l.Debit),
                    LatestTx = g.OrderByDescending(l => l.TransactionDate).ThenByDescending(l => l.CreatedOn).First()
                })
                .Where(x => x.Balance > 0)
                .ToList();

            if (!supplierBalances.Any()) return new List<PendingDueDto>();

            var supplierIds = supplierBalances.Select(d => d.SupplierId).ToList();
            var suppliers = await _context.Suppliers
                .AsNoTracking()
                .Where(s => supplierIds.Contains(s.Id) && s.CompanyId == finalCompanyId)
                .ToListAsync();

            return supplierBalances.Select(d => new PendingDueDto
            {
                SupplierId = d.SupplierId,
                PendingAmount = d.Balance,
                TotalAmount = d.Balance,
                SupplierName = suppliers.FirstOrDefault(s => s.Id == d.SupplierId)?.Name ?? "Unknown",
                Status = (d.LatestTx.TransactionDate.AddDays(15) < DateTime.Now) ? "Overdue" : "Active",
                DueDate = d.LatestTx.TransactionDate.AddDays(15),
                LastReferenceId = d.LatestTx.ReferenceId
            }).ToList();
        }

        public async Task<decimal> GetTotalPaymentsAsync(DateRangeDto dateRange)
        {
            var branchId = !string.IsNullOrEmpty(dateRange.BranchId) ? dateRange.BranchId : _branchId;
            if (branchId == "All Branches") branchId = null;
            Guid finalCompanyId = _companyId;
            if (!string.IsNullOrEmpty(dateRange.CompanyId) && Guid.TryParse(dateRange.CompanyId, out var cg)) finalCompanyId = cg;

            return await _context.SupplierPayments
                .Where(p => p.PaymentDate >= dateRange.StartDate && p.PaymentDate <= dateRange.EndDate && p.CompanyId == finalCompanyId && (string.IsNullOrEmpty(branchId) || p.BranchId == branchId))
                .SumAsync(p => p.Amount);
        }

        public async Task<AdjustmentsSummaryDto> GetTotalAdjustmentsAsync(DateRangeDto dateRange)
        {
            var branchId = !string.IsNullOrEmpty(dateRange.BranchId) ? dateRange.BranchId : _branchId;
            if (branchId == "All Branches") branchId = null;
            Guid finalCompanyId = _companyId;
            if (!string.IsNullOrEmpty(dateRange.CompanyId) && Guid.TryParse(dateRange.CompanyId, out var cg)) finalCompanyId = cg;

            // Debit note (Purchase returns/Shortages claims) = Sum of SupplierLedgers.Debit where ReferenceId starts with "LA-" and Debit > 0
            var debitAdjustments = await _context.SupplierLedgers
                .Where(l => l.TransactionDate >= dateRange.StartDate && l.TransactionDate <= dateRange.EndDate && l.CompanyId == finalCompanyId && (string.IsNullOrEmpty(branchId) || l.BranchId == branchId))
                .Where(l => l.ReferenceId != null && l.ReferenceId.StartsWith("LA-") && l.Debit > 0)
                .SumAsync(l => l.Debit);

            // Credit note (Rate differences/Purchases adjustments) = Sum of SupplierLedgers.Credit where ReferenceId starts with "LA-" and Credit > 0
            var creditAdjustments = await _context.SupplierLedgers
                .Where(l => l.TransactionDate >= dateRange.StartDate && l.TransactionDate <= dateRange.EndDate && l.CompanyId == finalCompanyId && (string.IsNullOrEmpty(branchId) || l.BranchId == branchId))
                .Where(l => l.ReferenceId != null && l.ReferenceId.StartsWith("LA-") && l.Credit > 0)
                .SumAsync(l => l.Credit);

            return new AdjustmentsSummaryDto
            {
                CreditAdjustments = creditAdjustments,
                DebitAdjustments = debitAdjustments
            };
        }

        public async Task<Dictionary<string, decimal>> GetGRNPaymentStatusesAsync(List<string> grnNumbers)
        {
            if (grnNumbers == null || !grnNumbers.Any()) return new Dictionary<string, decimal>();

            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            // 🔍 DEBUG LOGGING
            Console.WriteLine($"[Suppliers Debug] Fetching Payment Statuses. CompanyId: {_companyId}, BranchId: {_branchId}, GRN Count: {grnNumbers?.Count}");

            // Fetch all payments for this supplier (relevant to the search terms)
            // 🛡️ MULTI-BRANCH CONTEXT ENHANCEMENT: Ignore branch-level query filters for unique GRN/PO checks
            // GRNs/POs are unique across the entire company, so matching payments must be resolved company-wide.
            // Fallback: If _companyId is Guid.Empty (context propagation issue), allow company-wide match since GRNs/POs are globally unique.
            var relevantPayments = await _context.SupplierLedgers
                .IgnoreQueryFilters()
                .Where(l => (l.TransactionType == "Payment" || l.TransactionType == "Debit Note" || l.TransactionType == "Refund") && 
                            l.Description != null && 
                            (l.CompanyId == _companyId || _companyId == Guid.Empty))
                .Select(l => new { l.Description, l.ReferenceId, l.Debit, l.Credit, l.TransactionType })
                .ToListAsync();

            foreach (var grn in grnNumbers)
            {
                string cleanGrn = grn.Trim();
                var matchingTxns = relevantPayments
                    .Where(p => 
                        // Exact match in description with colon/space boundary or full match
                        (p.Description != null && (p.Description.Equals(cleanGrn, StringComparison.OrdinalIgnoreCase) || 
                                                 p.Description.Contains($": {cleanGrn} ", StringComparison.OrdinalIgnoreCase) || 
                                                 p.Description.Contains($" {cleanGrn}", StringComparison.OrdinalIgnoreCase) || 
                                                 p.Description.EndsWith($": {cleanGrn}", StringComparison.OrdinalIgnoreCase))) ||
                        // Exact match or prefix match in ReferenceId (for auto-gen suffixes)
                        (p.ReferenceId != null && (p.ReferenceId.Trim().Equals(cleanGrn, StringComparison.OrdinalIgnoreCase) || 
                                                 p.ReferenceId.Trim().StartsWith($"{cleanGrn}-", StringComparison.OrdinalIgnoreCase) || 
                                                 p.ReferenceId.Trim().StartsWith($"{cleanGrn}_", StringComparison.OrdinalIgnoreCase)))
                    )
                    .ToList();

                decimal totalPaid = matchingTxns.Where(t => t.TransactionType == "Payment" || t.TransactionType == "Debit Note").Sum(p => p.Debit)
                                  - matchingTxns.Where(t => t.TransactionType == "Refund").Sum(p => p.Credit);

                result[grn] = totalPaid;
            }

            return result;
        }

        public async Task<PaginatedListDto<PaymentReportDto>> GetPaymentsReportAsync(PaymentReportRequestDto request)
        {
            var branchId = !string.IsNullOrEmpty(request.BranchId) ? request.BranchId : _branchId;
            if (branchId == "All Branches") branchId = null;

            var query = from p in _context.SupplierPayments
                        join s in _context.Suppliers on p.SupplierId equals s.Id
                        where p.CompanyId == _companyId && s.CompanyId == _companyId && 
                              (string.IsNullOrEmpty(branchId) || p.BranchId == branchId) &&
                              (string.IsNullOrEmpty(branchId) || s.BranchId == branchId)
                        select new { p, s };

            query = query.Where(x => x.p.PaymentDate >= request.StartDate && x.p.PaymentDate <= request.EndDate);

            if (request.SupplierId.HasValue && request.SupplierId.Value != Guid.Empty)
            {
                query = query.Where(x => x.p.SupplierId == request.SupplierId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(x => 
                    x.s.Name.ToLower().Contains(searchTerm) || 
                    (x.p.ReferenceNumber != null && x.p.ReferenceNumber.ToLower().Contains(searchTerm)) || 
                    (x.p.Remarks != null && x.p.Remarks.ToLower().Contains(searchTerm)) ||
                    x.p.PaymentMode.ToLower().Contains(searchTerm)
                );
            }

            query = request.SortBy.ToLower() switch
            {
                "paymentdate" => request.SortOrder == "desc" ? query.OrderByDescending(x => x.p.PaymentDate) : query.OrderBy(x => x.p.PaymentDate),
                "amount" => request.SortOrder == "desc" ? query.OrderByDescending(x => x.p.Amount) : query.OrderBy(x => x.p.Amount),
                "suppliername" => request.SortOrder == "desc" ? query.OrderByDescending(x => x.s.Name) : query.OrderBy(x => x.s.Name),
                "referencenumber" => request.SortOrder == "desc" ? query.OrderByDescending(x => x.p.ReferenceNumber) : query.OrderBy(x => x.p.ReferenceNumber),
                _ => query.OrderByDescending(x => x.p.PaymentDate)
            };

            var totalCount = await query.CountAsync();

            var pagedResults = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var items = pagedResults.Select(x => new PaymentReportDto
            {
                Id = x.p.Id,
                SupplierId = x.p.SupplierId,
                SupplierName = x.s.Name,
                Amount = x.p.Amount,
                PaymentDate = x.p.PaymentDate,
                PaymentMode = x.p.PaymentMode,
                ReferenceNumber = x.p.ReferenceNumber,
                Remarks = x.p.Remarks,
                CreatedBy = x.p.CreatedBy,
                TransactionType = x.p.TransactionType
            }).ToList();

            return new PaginatedListDto<PaymentReportDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        public async Task<decimal> GetTotalPendingDuesAsync(string? branchId = null, string? companyId = null)
        {
            var finalBranchId = !string.IsNullOrEmpty(branchId) ? branchId : _branchId;
            if (finalBranchId == "All Branches") finalBranchId = null;
            Guid finalCompanyId = _companyId;
            if (!string.IsNullOrEmpty(companyId) && Guid.TryParse(companyId, out var cg)) finalCompanyId = cg;

            var allLedgerEntries = await _context.SupplierLedgers
                .Where(l => l.CompanyId == finalCompanyId && (string.IsNullOrEmpty(finalBranchId) || l.BranchId == finalBranchId))
                .OrderByDescending(l => l.TransactionDate)
                .ThenByDescending(l => l.CreatedOn)
                .ToListAsync();

            if (!allLedgerEntries.Any()) return 0;

            var supplierBalances = allLedgerEntries
                .GroupBy(l => l.SupplierId)
                .Select(g => g.First().Balance)
                .ToList();

            return supplierBalances.Where(b => b > 0).Sum();
        }

        public async Task<Dictionary<Guid, decimal>> GetSupplierBalancesAsync(List<Guid> supplierIds)
        {
            if (supplierIds == null || !supplierIds.Any()) return new Dictionary<Guid, decimal>();
 
            var allEntries = await _context.SupplierLedgers
                .Where(l => supplierIds.Contains(l.SupplierId) && l.CompanyId == _companyId && (l.BranchId == null || string.IsNullOrEmpty(_branchId) || l.BranchId == _branchId))
                .OrderByDescending(l => l.TransactionDate)
                .ThenByDescending(l => l.CreatedOn)
                .ToListAsync();

            return allEntries
                .GroupBy(l => l.SupplierId)
                .ToDictionary(
                    g => g.Key,
                    g => g.First().Balance
                );
        }

        public async Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(int months, string? branchId = null, string? companyId = null)
        {
            var finalBranchId = !string.IsNullOrEmpty(branchId) ? branchId : _branchId;
            if (finalBranchId == "All Branches") finalBranchId = null;
            Guid finalCompanyId = _companyId;
            if (!string.IsNullOrEmpty(companyId) && Guid.TryParse(companyId, out var cg)) finalCompanyId = cg;

            var startDate = DateTime.Now.AddMonths(-(months - 1));
            startDate = new DateTime(startDate.Year, startDate.Month, 1);

            var payments = await _context.SupplierPayments
                .Where(p => p.PaymentDate >= startDate && p.CompanyId == finalCompanyId && (string.IsNullOrEmpty(finalBranchId) || p.BranchId == finalBranchId))
                .ToListAsync();

            return payments
                .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                .Select(g => new MonthlyTrendDto
                {
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Amount = g.Sum(p => p.Amount)
                })
                .OrderBy(t => DateTime.Parse(t.Month))
                .ToList();
        }

        public async Task<bool> ReferenceExistsAsync(string referenceNumber)
        {
            if (string.IsNullOrWhiteSpace(referenceNumber)) return false;

            // Check if this reference is already used as a PAYMENT specifically
            return await _context.SupplierPayments.AnyAsync(r => r.ReferenceNumber == referenceNumber && r.CompanyId == _companyId && (r.BranchId == null || string.IsNullOrEmpty(_branchId) || r.BranchId == _branchId));
        }

        public async Task<bool> ChequeNumberExistsAsync(string chequeNumber, string bankName)
        {
            if (string.IsNullOrWhiteSpace(chequeNumber)) return false;

            // Check if this cheque number is already used in this bank in this company
            return await _context.SupplierPayments
                .IgnoreQueryFilters()
                .AnyAsync(p => p.ChequeNumber == chequeNumber && 
                               (p.BankName == bankName || string.IsNullOrEmpty(bankName)) && 
                               p.CompanyId == _companyId);
        }

        public async Task<bool> TransactionIdExistsAsync(string transactionId, string bankName)
        {
            if (string.IsNullOrWhiteSpace(transactionId)) return false;

            // Check if this transaction ID is already used in this bank in this company
            return await _context.SupplierPayments
                .IgnoreQueryFilters()
                .AnyAsync(p => p.TransactionId == transactionId && 
                               (p.BankName == bankName || string.IsNullOrEmpty(bankName)) && 
                               p.CompanyId == _companyId);
        }

        public async Task<List<SupplierPaymentDto>> GetPaymentsByReferencesAsync(List<string> referenceNumbers)
        {
            if (referenceNumbers == null || !referenceNumbers.Any()) return new List<SupplierPaymentDto>();

            // Collect clean references
            var cleanRefs = referenceNumbers.Select(r => r.Trim().ToLower()).ToList();

            var payments = await _context.SupplierPayments
                .Where(p => p.ReferenceNumber != null &&
                            p.CompanyId == _companyId &&
                            (p.BranchId == null || string.IsNullOrEmpty(_branchId) || p.BranchId == _branchId))
                .ToListAsync();

            // Perform in-memory matching to accommodate prefix or suffix formatting cleanly
            var matchingPayments = payments
                .Where(p => cleanRefs.Any(cr => 
                    p.ReferenceNumber!.ToLower().Equals(cr) ||
                    p.ReferenceNumber!.ToLower().StartsWith(cr + "-") ||
                    p.ReferenceNumber!.ToLower().StartsWith(cr + "_")
                ))
                .OrderBy(p => p.PaymentDate)
                .ToList();

            return matchingPayments.Select(p => new SupplierPaymentDto
            {
                SupplierId = p.SupplierId,
                CompanyId = p.CompanyId ?? Guid.Empty,
                BranchId = p.BranchId,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                PaymentMode = p.PaymentMode,
                ReferenceNumber = p.ReferenceNumber,
                Remarks = p.Remarks,
                TransactionType = p.TransactionType,
                CreatedBy = p.CreatedBy ?? "System",
                BankName = p.BankName,
                TransactionId = p.TransactionId,
                ChequeNumber = p.ChequeNumber,
                ChequeDate = p.ChequeDate,
                CreatedOn = p.CreatedOn
            }).ToList();
        }

        public async Task<bool> DeletePaymentAsync(Guid id)
        {
            var payment = await _context.SupplierPayments.FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == _companyId);
            
            SupplierLedger? ledgerEntry = null;

            if (payment == null)
            {
                // Try to check if the ID is a SupplierLedger.Id
                ledgerEntry = await _context.SupplierLedgers.FirstOrDefaultAsync(l => l.Id == id && l.CompanyId == _companyId);
                if (ledgerEntry != null)
                {
                    payment = await _context.SupplierPayments.FirstOrDefaultAsync(p => 
                        p.SupplierId == ledgerEntry.SupplierId &&
                        (p.ReferenceNumber == ledgerEntry.ReferenceId ||
                         (string.IsNullOrEmpty(p.ReferenceNumber) && 
                          (ledgerEntry.ReferenceId.StartsWith("PAY-") || ledgerEntry.ReferenceId.StartsWith("REF-")))) &&
                        (p.Amount == ledgerEntry.Debit || p.Amount == ledgerEntry.Credit) &&
                        p.CompanyId == _companyId);
                }
            }

            if (payment == null && ledgerEntry == null) return false;

            if (payment != null)
            {
                _context.SupplierPayments.Remove(payment);
            }

            if (ledgerEntry == null && payment != null)
            {
                var ledgerEntries = await _context.SupplierLedgers
                    .Where(l => l.SupplierId == payment.SupplierId &&
                                (l.Debit == payment.Amount || l.Credit == payment.Amount) &&
                                l.CompanyId == _companyId)
                    .ToListAsync();

                ledgerEntry = ledgerEntries
                    .OrderBy(l => l.ReferenceId == payment.ReferenceNumber ? 0 : 1)
                    .ThenBy(l => Math.Abs((l.TransactionDate - payment.PaymentDate).TotalSeconds))
                    .FirstOrDefault();
            }

            if (ledgerEntry != null)
            {
                var supplierId = ledgerEntry.SupplierId;
                _context.SupplierLedgers.Remove(ledgerEntry);

                await _context.SaveChangesAsync();

                // Recalculate running balances for this supplier
                var subsequentEntries = await _context.SupplierLedgers
                    .Where(l => l.SupplierId == supplierId && l.CompanyId == _companyId)
                    .OrderBy(l => l.CreatedOn)
                    .ToListAsync();

                decimal runningBalance = 0;
                foreach (var entry in subsequentEntries)
                {
                    runningBalance = runningBalance + entry.Credit - entry.Debit;
                    entry.Balance = runningBalance;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
