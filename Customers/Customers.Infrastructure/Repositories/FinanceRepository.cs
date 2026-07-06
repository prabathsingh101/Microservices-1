using Customers.Application.Common.Interfaces;
using Customers.Application.DTOs;
using Customers.Domain.Entities;
using Customers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Customers.Infrastructure.Repositories
{
    public class FinanceRepository : IFinanceRepository
    {
        private readonly CustomerDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly Guid _companyId;
        private readonly string? _branchId;

        public FinanceRepository(CustomerDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _companyId = _currentUserService.CompanyId ?? Guid.Empty;
            _branchId = _currentUserService.BranchId == "All Branches" ? null : _currentUserService.BranchId;
        }

        public async Task AddReceiptAsync(CustomerReceipt receipt)
        {
            receipt.CompanyId = _companyId;
            if (string.IsNullOrEmpty(receipt.BranchId))
            {
                receipt.BranchId = _branchId;
            }
            await _context.CustomerReceipts.AddAsync(receipt);
        }

        public async Task<CustomerLedger?> GetLastLedgerEntryAsync(Guid? customerId)
        {
            if (customerId == null || customerId == Guid.Empty) return null;

            return await _context.CustomerLedgers
                .Where(l => l.CustomerId == customerId && l.CompanyId == _companyId)
                .OrderByDescending(l => l.CreatedOn)
                .FirstOrDefaultAsync();
        }

        public async Task AddLedgerEntryAsync(CustomerLedger ledgerEntry)
        {
            ledgerEntry.CompanyId = _companyId;
            if (string.IsNullOrEmpty(ledgerEntry.BranchId))
            {
                ledgerEntry.BranchId = _branchId;
            }
            await _context.CustomerLedgers.AddAsync(ledgerEntry);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<CustomerLedgerPagedResultDto> GetLedgerAsync(CustomerLedgerRequestDto request)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId && c.CompanyId == _companyId);
            var query = _context.CustomerLedgers.Where(l => l.CustomerId == request.CustomerId && l.CompanyId == _companyId);

            if (request.StartDate.HasValue)
                query = query.Where(l => l.TransactionDate >= request.StartDate.Value);
            if (request.EndDate.HasValue)
                query = query.Where(l => l.TransactionDate <= request.EndDate.Value);

            if (!string.IsNullOrWhiteSpace(request.TypeFilter))
            {
                var type = request.TypeFilter.ToLower();
                query = query.Where(l => l.TransactionType.ToLower().Contains(type));
            }

            if (!string.IsNullOrWhiteSpace(request.ReferenceFilter))
            {
                var refId = request.ReferenceFilter.ToLower();
                query = query.Where(l => l.ReferenceId != null && l.ReferenceId.ToLower().Contains(refId));
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(l => 
                    l.TransactionType.ToLower().Contains(term) || 
                    (l.ReferenceId != null && l.ReferenceId.ToLower().Contains(term)) || 
                    (l.Description != null && l.Description.ToLower().Contains(term))
                );
            }

            query = request.SortBy.ToLower() switch
            {
                "transactiondate" => request.SortOrder == "desc" ? query.OrderByDescending(l => l.TransactionDate) : query.OrderBy(l => l.TransactionDate),
                "transactiontype" => request.SortOrder == "desc" ? query.OrderByDescending(l => l.TransactionType) : query.OrderBy(l => l.TransactionType),
                "referenceid" => request.SortOrder == "desc" ? query.OrderByDescending(l => l.ReferenceId) : query.OrderBy(l => l.ReferenceId),
                "debit" => request.SortOrder == "desc" ? query.OrderByDescending(l => l.Debit) : query.OrderBy(l => l.Debit),
                "credit" => request.SortOrder == "desc" ? query.OrderByDescending(l => l.Credit) : query.OrderBy(l => l.Credit),
                "balance" => request.SortOrder == "desc" ? query.OrderByDescending(l => l.Balance) : query.OrderBy(l => l.Balance),
                _ => query.OrderByDescending(l => l.TransactionDate)
            };

            var totalCount = await query.CountAsync();
            var currentBalance = await _context.CustomerLedgers
                .Where(l => l.CustomerId == request.CustomerId && l.CompanyId == _companyId)
                .OrderByDescending(l => l.CreatedOn)
                .Select(l => l.Balance)
                .FirstOrDefaultAsync();

            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new CustomerLedgerPagedResultDto
            {
                CustomerName = customer?.CustomerName ?? "Unknown",
                CurrentBalance = currentBalance,
                Ledger = new PaginatedListDto<CustomerLedger>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                }
            };
        }

        public async Task<OutstandingPagedResultDto> GetOutstandingAsync(OutstandingRequestDto request)
        {
            var internalAccountNames = new[] { 
                "Proprietor (Self / Capital Account)", 
                "Company Bank Account (Internal)" 
            };

            var latestEntries = _context.CustomerLedgers
                .Where(l => l.CompanyId == _companyId)
                .Where(l => l.CreatedOn == _context.CustomerLedgers
                    .Where(inner => inner.CustomerId == l.CustomerId && inner.CompanyId == _companyId)
                    .Max(inner => inner.CreatedOn))
                .Where(l => l.Balance > 0);

            var query = from l in latestEntries
                        join c in _context.Customers on l.CustomerId equals c.Id
                        where !internalAccountNames.Contains(c.CustomerName!)
                        select new OutstandingDto
                        {
                            CustomerId = l.CustomerId,
                            CustomerName = c.CustomerName,
                            PendingAmount = l.Balance,
                            TotalAmount = l.Balance, 
                            Status = (l.TransactionDate.AddDays(15) < DateTime.Now) ? "Overdue" : "Active",
                            DueDate = l.TransactionDate.AddDays(15), 
                            LastReferenceId = l.ReferenceId
                        };

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(o => o.CustomerName!.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(request.CustomerNameFilter))
            {
                var name = request.CustomerNameFilter.ToLower();
                query = query.Where(o => o.CustomerName!.ToLower().Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(request.StatusFilter))
            {
                var status = request.StatusFilter.ToLower();
                query = query.Where(o => o.Status!.ToLower().Contains(status));
            }

            query = request.SortBy.ToLower() switch
            {
                "customername" => request.SortOrder == "desc" ? query.OrderByDescending(o => o.CustomerName) : query.OrderBy(o => o.CustomerName),
                "pendingamount" => request.SortOrder == "desc" ? query.OrderByDescending(o => o.PendingAmount) : query.OrderBy(o => o.PendingAmount),
                "totalamount" => request.SortOrder == "desc" ? query.OrderByDescending(o => o.TotalAmount) : query.OrderBy(o => o.TotalAmount),
                "status" => request.SortOrder == "desc" ? query.OrderByDescending(o => o.Status) : query.OrderBy(o => o.Status),
                "duedate" => request.SortOrder == "desc" ? query.OrderByDescending(o => o.DueDate) : query.OrderBy(o => o.DueDate),
                _ => request.SortOrder == "desc" ? query.OrderByDescending(o => o.CustomerName) : query.OrderBy(o => o.CustomerName)
            };

            var totalCount = await query.CountAsync();
            var totalAmount = await query.SumAsync(o => o.PendingAmount);

            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new OutstandingPagedResultDto
            {
                TotalOutstandingAmount = totalAmount,
                Items = new PaginatedListDto<OutstandingDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                }
            };
        }

        public async Task<decimal> GetTotalReceiptsAsync(DateRangeDto dateRange)
        {
            string? branchGuid = string.IsNullOrEmpty(dateRange.BranchId) ? _branchId : dateRange.BranchId;
            if (branchGuid == "All Branches") branchGuid = null;

            Guid finalCompanyId = _companyId;
            if (!string.IsNullOrEmpty(dateRange.CompanyId) && Guid.TryParse(dateRange.CompanyId, out var cg)) finalCompanyId = cg;

            return await _context.CustomerReceipts
                .Where(r => r.ReceiptDate >= dateRange.StartDate && r.ReceiptDate <= dateRange.EndDate && r.CompanyId == finalCompanyId)
                .Where(r => r.BranchId == null || string.IsNullOrEmpty(branchGuid) || r.BranchId == branchGuid)
                .Where(r => r.ReceiptMode != "Adjustment" && (r.Remarks == null || (!r.Remarks.Contains("[PROPRIETOR_CAPITAL]") && !r.Remarks.Contains("[BANK_TRANSFER]"))))
                .SumAsync(r => r.Amount);
        }

        public async Task<AdjustmentsSummaryDto> GetTotalAdjustmentsAsync(DateRangeDto dateRange)
        {
            string? branchGuid = string.IsNullOrEmpty(dateRange.BranchId) ? _branchId : dateRange.BranchId;
            if (branchGuid == "All Branches") branchGuid = null;

            Guid finalCompanyId = _companyId;
            if (!string.IsNullOrEmpty(dateRange.CompanyId) && Guid.TryParse(dateRange.CompanyId, out var cg)) finalCompanyId = cg;

            // Credit adjustments represent customer credit notes (write-offs/bad debts)
            // They are recorded in CustomerReceipts with ReceiptMode == "Adjustment"
            var creditAdjustments = await _context.CustomerReceipts
                .Where(r => r.ReceiptDate >= dateRange.StartDate && r.ReceiptDate <= dateRange.EndDate && r.CompanyId == finalCompanyId)
                .Where(r => r.BranchId == null || string.IsNullOrEmpty(branchGuid) || r.BranchId == branchGuid)
                .Where(r => r.ReceiptMode == "Adjustment")
                .SumAsync(r => r.Amount);

            // Debit adjustments represent customer debit notes (finance charges, surcharges)
            // They are recorded in CustomerLedger with ReferenceId starting with "LA-" and have a Debit amount
            var debitAdjustments = await _context.CustomerLedgers
                .Where(l => l.TransactionDate >= dateRange.StartDate && l.TransactionDate <= dateRange.EndDate && l.CompanyId == finalCompanyId)
                .Where(l => l.BranchId == null || string.IsNullOrEmpty(branchGuid) || l.BranchId == branchGuid)
                .Where(l => l.ReferenceId != null && l.ReferenceId.StartsWith("LA-") && l.Debit > 0)
                .SumAsync(l => l.Debit);

            return new AdjustmentsSummaryDto
            {
                CreditAdjustments = creditAdjustments,
                DebitAdjustments = debitAdjustments
            };
        }

        public async Task<decimal> GetTotalOutstandingAsync(string? branchId = null, string? companyId = null)
        {
            string? finalBranchId = string.IsNullOrEmpty(branchId) ? _branchId : branchId;
            if (finalBranchId == "All Branches") finalBranchId = null;
            Guid finalCompanyId = _companyId;
            if (!string.IsNullOrEmpty(companyId) && Guid.TryParse(companyId, out var cg)) finalCompanyId = cg;

            var internalAccountNames = new[] { 
                "Proprietor (Self / Capital Account)", 
                "Company Bank Account (Internal)" 
            };
            
            var customerBalances = await _context.CustomerLedgers
                .Where(l => l.CompanyId == finalCompanyId && (l.BranchId == null || string.IsNullOrEmpty(finalBranchId) || l.BranchId == finalBranchId))
                .Where(l => l.CreatedOn == _context.CustomerLedgers
                    .Where(inner => inner.CustomerId == l.CustomerId && inner.CompanyId == finalCompanyId && (inner.BranchId == null || string.IsNullOrEmpty(finalBranchId) || inner.BranchId == finalBranchId))
                    .Max(inner => inner.CreatedOn))
                .Join(_context.Customers, l => l.CustomerId, c => c.Id, (l, c) => new { l, c })
                .Where(x => !internalAccountNames.Contains(x.c.CustomerName!) && x.c.CompanyId == finalCompanyId && (x.c.BranchId == null || string.IsNullOrEmpty(finalBranchId) || x.c.BranchId == finalBranchId))
                .Select(x => x.l.Balance)
                .ToListAsync();

            return customerBalances.Where(b => b > 0).Sum();
        }

        public async Task<List<OutstandingDto>> GetPendingDuesAsync(string? branchId = null, string? companyId = null)
        {
            string? finalBranchId = string.IsNullOrEmpty(branchId) ? _branchId : branchId;
            if (finalBranchId == "All Branches") finalBranchId = null;

            Guid finalCompanyId = _companyId;
            if (!string.IsNullOrEmpty(companyId) && Guid.TryParse(companyId, out var cg)) finalCompanyId = cg;

            var latestEntries = await _context.CustomerLedgers
                .Where(l => l.CompanyId == finalCompanyId && (l.BranchId == null || string.IsNullOrEmpty(finalBranchId) || l.BranchId == finalBranchId))
                .Where(l => l.CreatedOn == _context.CustomerLedgers
                    .Where(inner => inner.CustomerId == l.CustomerId && inner.CompanyId == finalCompanyId && (inner.BranchId == null || string.IsNullOrEmpty(finalBranchId) || inner.BranchId == finalBranchId))
                    .Max(inner => inner.CreatedOn))
                .Where(l => l.Balance > 0)
                .ToListAsync();

            var customerIds = latestEntries.Select(d => d.CustomerId).ToList();
            var customers = await _context.Customers.Where(c => customerIds.Contains(c.Id) && (c.BranchId == null || string.IsNullOrEmpty(finalBranchId) || c.BranchId == finalBranchId)).ToListAsync();

            return latestEntries.Select(l => {
                var c = customers.FirstOrDefault(c => c.Id == l.CustomerId);
                return new OutstandingDto
                {
                    CustomerId = l.CustomerId ?? Guid.Empty,
                    CustomerName = c?.CustomerName,
                    Phone = c?.Phone,
                    PendingAmount = l.Balance,
                    TotalAmount = l.Balance,
                    Status = (l.TransactionDate.AddDays(15) < DateTime.Now) ? "Overdue" : "Active",
                    DueDate = l.TransactionDate.AddDays(15),
                    LastReferenceId = l.ReferenceId
                };
            }).ToList();
        }

        public async Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(int months, string? branchId = null, string? companyId = null)
        {
            string? finalBranchId = string.IsNullOrEmpty(branchId) ? _branchId : branchId;
            if (finalBranchId == "All Branches") finalBranchId = null;

            Guid finalCompanyId = _companyId;
            if (!string.IsNullOrEmpty(companyId) && Guid.TryParse(companyId, out var cg)) finalCompanyId = cg;

            var startDate = DateTime.Now.AddMonths(-(months - 1));
            startDate = new DateTime(startDate.Year, startDate.Month, 1);

            var receipts = await _context.CustomerReceipts
                .Where(r => r.ReceiptDate >= startDate && r.CompanyId == finalCompanyId)
                .Where(r => r.BranchId == null || string.IsNullOrEmpty(finalBranchId) || r.BranchId == finalBranchId)
                .Where(r => r.ReceiptMode != "Adjustment" && (r.Remarks == null || (!r.Remarks.Contains("[PROPRIETOR_CAPITAL]") && !r.Remarks.Contains("[BANK_TRANSFER]"))))
                .ToListAsync();

            return receipts
                .GroupBy(r => new { r.ReceiptDate.Year, r.ReceiptDate.Month })
                .Select(g => new MonthlyTrendDto
                {
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Amount = g.Sum(r => r.Amount)
                })
                .OrderBy(t => DateTime.Parse(t!.Month!))
                .ToList();
        }

        public async Task<bool> IsReferenceUniqueAsync(string referenceNumber)
        {
            var (isUnique, _) = await IsReferenceUniqueWithSourceAsync(referenceNumber);
            return isUnique;
        }

        public async Task<(bool IsUnique, string Source)> IsReferenceUniqueWithSourceAsync(string referenceNumber)
        {
            if (string.IsNullOrWhiteSpace(referenceNumber)) return (true, string.Empty);
            
            bool existsInReceipts = await _context.CustomerReceipts.AnyAsync(r => r.ReferenceNumber == referenceNumber && r.CompanyId == _companyId && (r.BranchId == null || string.IsNullOrEmpty(_branchId) || r.BranchId == _branchId));
            if (existsInReceipts) return (false, "Receipts");

            // Allow payments to reference the Sales Order (SO-XXXX) that has been recorded in the Customer Ledger.
            if (referenceNumber.StartsWith("SO-", StringComparison.OrdinalIgnoreCase) || referenceNumber.StartsWith("SO-Q-", StringComparison.OrdinalIgnoreCase))
            {
                return (true, string.Empty);
            }

            bool existsInLedger = await _context.CustomerLedgers.AnyAsync(l => l.ReferenceId == referenceNumber && l.CompanyId == _companyId && (l.BranchId == null || string.IsNullOrEmpty(_branchId) || l.BranchId == _branchId));
            if (existsInLedger) return (false, "Customer Ledgers");

            return (true, string.Empty);
        }

        public async Task<bool> HasRefundOrAdjustmentAgainstReferenceAsync(Guid customerId, string referenceNumber)
        {
            if (string.IsNullOrWhiteSpace(referenceNumber)) return false;

            // Check if there is already a Refund entry or an Adjustment entry in the ledger against this specific reference number
            var duplicateLedgerExists = await _context.CustomerLedgers.AnyAsync(l => 
                l.CustomerId == customerId && 
                l.ReferenceId == referenceNumber && 
                (
                    l.TransactionType == "Refund" || 
                    (l.Description != null && (l.Description.Contains("Adjustment") || l.Description.Contains("Refund"))) 
                ) && 
                l.CompanyId == _companyId
            );

            if (duplicateLedgerExists) return true;

            // Also check Receipts where Amount < 0 (Refunds recorded as negative receipts)
            var duplicateReceiptExists = await _context.CustomerReceipts.AnyAsync(r =>
                r.CustomerId == customerId &&
                r.ReferenceNumber == referenceNumber &&
                r.Amount < 0 &&
                r.CompanyId == _companyId
            );

            return duplicateReceiptExists;
        }

        public async Task<PaginatedListDto<ReceiptReportDto>> GetReceiptsReportAsync(ReceiptReportRequestDto request)
        {
            string? branchGuid = string.IsNullOrEmpty(request.BranchId) ? _branchId : request.BranchId;

            var query = _context.CustomerReceipts.Where(r => r.CompanyId == _companyId && (r.BranchId == null || string.IsNullOrEmpty(branchGuid) || r.BranchId == branchGuid)).AsQueryable();

            query = query.Where(r => r.ReceiptDate >= request.StartDate && r.ReceiptDate <= request.EndDate);

            if (request.CustomerId.HasValue && request.CustomerId.Value != Guid.Empty)
                query = query.Where(r => r.CustomerId == request.CustomerId.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(r => 
                    (r.ReferenceNumber != null && r.ReferenceNumber.ToLower().Contains(term)) || 
                    (r.Remarks != null && r.Remarks.ToLower().Contains(term))
                );
            }

            var totalCount = await query.CountAsync();

            var pagedResults = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var customerIds = pagedResults.Select(p => p.CustomerId).Distinct().ToList();
            var customers = await _context.Customers.Where(c => customerIds.Contains(c.Id) && (c.BranchId == null || string.IsNullOrEmpty(_branchId) || c.BranchId == _branchId)).ToDictionaryAsync(c => c.Id, c => c.CustomerName);

            var items = pagedResults.Select(r => new ReceiptReportDto
            {
                Id = r.Id,
                CustomerId = r.CustomerId ?? Guid.Empty,
                CustomerName = (r.CustomerId.HasValue && customers.ContainsKey(r.CustomerId.Value)) ? customers[r.CustomerId.Value] : "Walking Customer",
                Amount = r.Amount,
                ReceiptDate = r.ReceiptDate,
                ReceiptMode = r.ReceiptMode,
                ReferenceNumber = r.ReferenceNumber,
                Remarks = r.Remarks,
                CreatedBy = r.CreatedBy,
                TransactionType = r.Amount < 0 ? "Refund" : "Receipt"
            }).ToList();

            return new PaginatedListDto<ReceiptReportDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        // --- NEW FEATURES ---
        public async Task<List<DebtorsAgeingDto>> GetDebtorsAgeingAsync(string? branchId = null)
        {
            string? finalBranchId = string.IsNullOrEmpty(branchId) ? _branchId : branchId;
            if (finalBranchId == "All Branches") finalBranchId = null;

            var ledgers = await _context.CustomerLedgers
                .Where(l => l.CompanyId == _companyId && (l.BranchId == null || string.IsNullOrEmpty(finalBranchId) || l.BranchId == finalBranchId))
                .OrderBy(l => l.TransactionDate)
                .ToListAsync();

            var customers = await _context.Customers
                .Where(c => c.CompanyId == _companyId && (c.BranchId == null || string.IsNullOrEmpty(finalBranchId) || c.BranchId == finalBranchId))
                .ToListAsync();

            var result = new List<DebtorsAgeingDto>();

            var internalAccountNames = new[] { 
                "Proprietor (Self / Capital Account)", 
                "Company Bank Account (Internal)" 
            };

            foreach (var customer in customers)
            {
                if (internalAccountNames.Contains(customer.CustomerName!)) continue;

                var customerLedger = ledgers.Where(l => l.CustomerId == customer.Id).ToList();
                if (!customerLedger.Any()) continue;

                var lastEntry = customerLedger.OrderByDescending(l => l.CreatedOn).FirstOrDefault();
                if (lastEntry == null || lastEntry.Balance <= 0) continue;

                decimal totalBalance = lastEntry.Balance;

                var debitEntries = customerLedger.Where(l => l.Debit > 0).OrderBy(l => l.TransactionDate).ToList();
                var creditEntries = customerLedger.Where(l => l.Credit > 0).OrderBy(l => l.TransactionDate).ToList();

                decimal totalCredits = creditEntries.Sum(l => l.Credit);

                decimal age0To30 = 0;
                decimal age31To60 = 0;
                decimal age61To90 = 0;
                decimal age91Plus = 0;

                foreach (var debit in debitEntries)
                {
                    decimal remainingDebit = debit.Debit;
                    if (totalCredits > 0)
                    {
                        if (totalCredits >= remainingDebit)
                        {
                            totalCredits -= remainingDebit;
                            remainingDebit = 0;
                        }
                        else
                        {
                            remainingDebit -= totalCredits;
                            totalCredits = 0;
                        }
                    }

                    if (remainingDebit > 0)
                    {
                        var ageInDays = (DateTime.Now - debit.TransactionDate).TotalDays;
                        if (ageInDays <= 30)
                            age0To30 += remainingDebit;
                        else if (ageInDays <= 60)
                            age31To60 += remainingDebit;
                        else if (ageInDays <= 90)
                            age61To90 += remainingDebit;
                        else
                            age91Plus += remainingDebit;
                    }
                }

                decimal sumOfBuckets = age0To30 + age31To60 + age61To90 + age91Plus;
                if (sumOfBuckets != totalBalance && sumOfBuckets > 0)
                {
                    decimal ratio = totalBalance / sumOfBuckets;
                    age0To30 *= ratio;
                    age31To60 *= ratio;
                    age61To90 *= ratio;
                    age91Plus *= ratio;
                }
                else if (sumOfBuckets == 0)
                {
                    age0To30 = totalBalance;
                }

                result.Add(new DebtorsAgeingDto
                {
                    CustomerId = customer.Id,
                    CustomerName = customer.CustomerName,
                    Phone = customer.Phone,
                    TotalOutstanding = totalBalance,
                    Age0To30 = Math.Round(age0To30, 2),
                    Age31To60 = Math.Round(age31To60, 2),
                    Age61To90 = Math.Round(age61To90, 2),
                    Age91Plus = Math.Round(age91Plus, 2)
                });
            }

            return result;
        }

        public async Task RecordPaymentReminderAsync(PaymentReminderLog log)
        {
            log.CompanyId = _companyId;
            if (string.IsNullOrEmpty(log.BranchId))
            {
                log.BranchId = _branchId;
            }
            await _context.PaymentReminderLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<PaymentReminderLogDto>> GetPaymentReminderLogsAsync(Guid? customerId = null, string? branchId = null)
        {
            string? finalBranchId = string.IsNullOrEmpty(branchId) ? _branchId : branchId;
            if (finalBranchId == "All Branches") finalBranchId = null;

            var query = _context.PaymentReminderLogs
                .Where(l => l.CompanyId == _companyId && (l.BranchId == null || string.IsNullOrEmpty(finalBranchId) || l.BranchId == finalBranchId));

            if (customerId.HasValue && customerId.Value != Guid.Empty)
            {
                query = query.Where(l => l.CustomerId == customerId.Value);
            }

            return await query
                .OrderByDescending(l => l.CreatedOn)
                .Select(l => new PaymentReminderLogDto
                {
                    CustomerId = l.CustomerId,
                    CustomerName = l.CustomerName,
                    Phone = l.Phone,
                    OutstandingAmount = l.OutstandingAmount,
                    ReminderType = l.ReminderType,
                    SentStatus = l.SentStatus,
                    SentMessage = l.SentMessage,
                    CreatedOn = l.CreatedOn ?? DateTime.MinValue
                })
                .ToListAsync();
        }

        public async Task RecordContraEntryAsync(ContraEntry contra)
        {
            contra.CompanyId = _companyId;
            if (string.IsNullOrEmpty(contra.BranchId))
            {
                contra.BranchId = _branchId;
            }
            contra.TransferDate = DateTime.Now;
            
            await _context.ContraEntries.AddAsync(contra);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ContraEntryDto>> GetContraEntriesAsync(string? branchId = null)
        {
            string? finalBranchId = string.IsNullOrEmpty(branchId) ? _branchId : branchId;
            if (finalBranchId == "All Branches") finalBranchId = null;

            return await _context.ContraEntries
                .Where(c => c.CompanyId == _companyId && (c.BranchId == null || string.IsNullOrEmpty(finalBranchId) || c.BranchId == finalBranchId))
                .OrderByDescending(c => c.TransferDate)
                .Select(c => new ContraEntryDto
                {
                    Id = c.Id,
                    TransferDate = c.TransferDate,
                    SourceType = c.SourceType,
                    SourceAccount = c.SourceAccount,
                    DestinationType = c.DestinationType,
                    DestinationAccount = c.DestinationAccount,
                    Amount = c.Amount,
                    ReferenceNumber = c.ReferenceNumber,
                    Remarks = c.Remarks,
                    CreatedOn = c.CreatedOn ?? DateTime.MinValue
                })
                .ToListAsync();
        }

        public async Task UploadBankStatementAsync(BankStatement statement, List<BankStatementLine> lines)
        {
            statement.CompanyId = _companyId;
            if (string.IsNullOrEmpty(statement.BranchId))
            {
                statement.BranchId = _branchId;
            }
            statement.UploadDate = DateTime.Now;
            statement.Status = "Pending";

            await _context.BankStatements.AddAsync(statement);
            await _context.SaveChangesAsync();

            foreach (var line in lines)
            {
                line.BankStatementId = statement.Id;
                line.ReconciliationStatus = "Unmatched";
                await _context.BankStatementLines.AddAsync(line);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<List<BankStatementDto>> GetBankStatementsAsync(string? branchId = null)
        {
            string? finalBranchId = string.IsNullOrEmpty(branchId) ? _branchId : branchId;
            if (finalBranchId == "All Branches") finalBranchId = null;

            return await _context.BankStatements
                .Where(s => s.CompanyId == _companyId && (s.BranchId == null || string.IsNullOrEmpty(finalBranchId) || s.BranchId == finalBranchId))
                .OrderByDescending(s => s.UploadDate)
                .Select(s => new BankStatementDto
                {
                    Id = s.Id,
                    FileName = s.FileName,
                    UploadDate = s.UploadDate,
                    BankAccountNumber = s.BankAccountNumber,
                    BankName = s.BankName,
                    Status = s.Status,
                    TotalAmount = s.TotalAmount
                })
                .ToListAsync();
        }

        public async Task<List<BankStatementLineDto>> GetBankStatementLinesAsync(Guid statementId)
        {
            return await _context.BankStatementLines
                .Where(l => l.BankStatementId == statementId)
                .OrderBy(l => l.TransactionDate)
                .Select(l => new BankStatementLineDto
                {
                    Id = l.Id,
                    BankStatementId = l.BankStatementId,
                    TransactionDate = l.TransactionDate,
                    Description = l.Description,
                    ReferenceNumber = l.ReferenceNumber,
                    Withdrawal = l.Withdrawal,
                    Deposit = l.Deposit,
                    ReconciliationStatus = l.ReconciliationStatus,
                    MatchedTransactionType = l.MatchedTransactionType,
                    MatchedTransactionId = l.MatchedTransactionId
                })
                .ToListAsync();
        }

        public async Task<List<ReceiptReportDto>> GetUnmatchedSystemTransactionsAsync(string transactionType, string? branchId = null)
        {
            string? finalBranchId = string.IsNullOrEmpty(branchId) ? _branchId : branchId;
            if (finalBranchId == "All Branches") finalBranchId = null;

            var matchedIds = await _context.BankStatementLines
                .Where(l => l.ReconciliationStatus == "Matched" && l.MatchedTransactionType == transactionType && l.MatchedTransactionId != null)
                .Select(l => l.MatchedTransactionId!.Value)
                .ToListAsync();

            if (transactionType == "CustomerReceipt")
            {
                var receipts = await _context.CustomerReceipts
                    .Where(r => r.CompanyId == _companyId && (r.BranchId == null || string.IsNullOrEmpty(finalBranchId) || r.BranchId == finalBranchId))
                    .Where(r => !matchedIds.Contains(r.Id))
                    .OrderByDescending(r => r.ReceiptDate)
                    .ToListAsync();

                var customerIds = receipts.Select(r => r.CustomerId).Distinct().ToList();
                var customers = await _context.Customers.Where(c => customerIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.CustomerName);

                return receipts.Select(r => new ReceiptReportDto
                {
                    Id = r.Id,
                    CustomerId = r.CustomerId ?? Guid.Empty,
                    CustomerName = (r.CustomerId.HasValue && customers.ContainsKey(r.CustomerId.Value)) ? customers[r.CustomerId.Value] : "Walking Customer",
                    Amount = r.Amount,
                    ReceiptDate = r.ReceiptDate,
                    ReceiptMode = r.ReceiptMode,
                    ReferenceNumber = r.ReferenceNumber,
                    Remarks = r.Remarks,
                    CreatedBy = r.CreatedBy,
                    TransactionType = r.Amount < 0 ? "Refund" : "Receipt"
                }).ToList();
            }

            return new List<ReceiptReportDto>();
        }

        public async Task<bool> ReconcileTransactionAsync(Guid lineId, string matchedTransactionType, Guid matchedTransactionId)
        {
            var line = await _context.BankStatementLines.FirstOrDefaultAsync(l => l.Id == lineId);
            if (line == null) return false;

            line.ReconciliationStatus = "Matched";
            line.MatchedTransactionType = matchedTransactionType;
            line.MatchedTransactionId = matchedTransactionId;

            await _context.SaveChangesAsync();

            var statementId = line.BankStatementId;
            var allLines = await _context.BankStatementLines.Where(l => l.BankStatementId == statementId).ToListAsync();
            var statement = await _context.BankStatements.FirstOrDefaultAsync(s => s.Id == statementId);
            
            if (statement != null)
            {
                int matchedCount = allLines.Count(l => l.ReconciliationStatus == "Matched");
                if (matchedCount == allLines.Count)
                {
                    statement.Status = "Reconciled";
                }
                else if (matchedCount > 0)
                {
                    statement.Status = "Partially Reconciled";
                }
                else
                {
                    statement.Status = "Pending";
                }
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<bool> ChequeNumberExistsAsync(string chequeNumber, string bankName)
        {
            if (string.IsNullOrWhiteSpace(chequeNumber)) return false;
            
            return await _context.CustomerReceipts.AnyAsync(r => 
                r.ChequeNumber == chequeNumber && 
                r.BankName == bankName && 
                r.CompanyId == _companyId);
        }

        public async Task<bool> DeleteReceiptAsync(Guid id)
        {
            // 1. Try to find by CustomerReceipt.Id
            var receipt = await _context.CustomerReceipts.FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == _companyId);
            
            CustomerLedger? ledgerEntry = null;

            if (receipt == null)
            {
                // 2. If not found, check if the ID is a CustomerLedger.Id
                ledgerEntry = await _context.CustomerLedgers.FirstOrDefaultAsync(l => l.Id == id && l.CompanyId == _companyId);
                if (ledgerEntry != null)
                {
                    // Find matching receipt by reference, amount, and customer
                    receipt = await _context.CustomerReceipts.FirstOrDefaultAsync(r => 
                        r.CustomerId == ledgerEntry.CustomerId &&
                        (r.ReferenceNumber == ledgerEntry.ReferenceId || 
                         (string.IsNullOrEmpty(r.ReferenceNumber) && 
                          (ledgerEntry.ReferenceId.StartsWith("REC-") || ledgerEntry.ReferenceId.StartsWith("REF-")))) &&
                        (r.Amount == ledgerEntry.Credit || r.Amount == -ledgerEntry.Debit) &&
                        r.CompanyId == _companyId);
                }
            }

            if (receipt == null && ledgerEntry == null) return false;

            // Delete receipt if found
            if (receipt != null)
            {
                _context.CustomerReceipts.Remove(receipt);
            }

            // If we haven't resolved the ledger entry yet, find it
            if (ledgerEntry == null && receipt != null)
            {
                var ledgerEntries = await _context.CustomerLedgers
                    .Where(l => l.CustomerId == receipt.CustomerId &&
                                (l.Credit == Math.Max(0, receipt.Amount) && l.Debit == Math.Max(0, -receipt.Amount)) &&
                                l.CompanyId == _companyId)
                    .ToListAsync();

                ledgerEntry = ledgerEntries
                    .OrderBy(l => l.ReferenceId == receipt.ReferenceNumber ? 0 : 1)
                    .ThenBy(l => Math.Abs((l.TransactionDate - receipt.ReceiptDate).TotalSeconds))
                    .FirstOrDefault();
            }

            if (ledgerEntry != null)
            {
                var customerId = ledgerEntry.CustomerId;
                var deletedEntryId = ledgerEntry.Id;

                // Delete the ledger entry
                _context.CustomerLedgers.Remove(ledgerEntry);

                // Save the removal first so it's not retrieved in subsequent query
                await _context.SaveChangesAsync();

                // Recalculate running balances for this customer
                var subsequentEntries = await _context.CustomerLedgers
                    .Where(l => l.CustomerId == customerId && l.CompanyId == _companyId)
                    .OrderBy(l => l.CreatedOn)
                    .ToListAsync();

                decimal runningBalance = 0;
                foreach (var entry in subsequentEntries)
                {
                    runningBalance = runningBalance + entry.Debit - entry.Credit;
                    entry.Balance = runningBalance;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
