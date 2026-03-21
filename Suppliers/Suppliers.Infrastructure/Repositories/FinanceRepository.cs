using Microsoft.EntityFrameworkCore;
using Suppliers.Application.DTOs;
using Suppliers.Application.Interfaces;
using Suppliers.Domain.Entities;

namespace Suppliers.Infrastructure.Repositories // Adjust namespace if needed
{
    public class FinanceRepository(SupplierDbContext context) : IFinanceRepository
    {
        private readonly SupplierDbContext _context = context;

        public async Task AddPaymentAsync(SupplierPayment payment)
        {
            await _context.SupplierPayments.AddAsync(payment);
        }

        public async Task<SupplierLedger?> GetLastLedgerEntryAsync(int supplierId)
        {
            return await _context.SupplierLedgers
                .Where(l => l.SupplierId == supplierId)
                .OrderByDescending(l => l.Id)
                .FirstOrDefaultAsync();
        }

        public async Task AddLedgerEntryAsync(SupplierLedger ledgerEntry)
        {
            await _context.SupplierLedgers.AddAsync(ledgerEntry);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<SupplierLedgerPagedResultDto> GetLedgerAsync(SupplierLedgerRequestDto request)
        {
            var supplier = await _context.Suppliers.FindAsync(request.SupplierId);
            var query = _context.SupplierLedgers.Where(l => l.SupplierId == request.SupplierId);

            // 1. Date Filtering
            if (request.StartDate.HasValue)
                query = query.Where(l => l.TransactionDate >= request.StartDate.Value);
            if (request.EndDate.HasValue)
                query = query.Where(l => l.TransactionDate <= request.EndDate.Value);

            // 1.1 Column Specific Filters
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

            // 2. Searching (Global)
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(l => 
                    l.TransactionType.ToLower().Contains(term) || 
                    (l.ReferenceId != null && l.ReferenceId.ToLower().Contains(term)) || 
                    (l.Description != null && l.Description.ToLower().Contains(term))
                );
            }

            // 3. Sorting
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

            // 4. Counts and Summaries
            var totalCount = await query.CountAsync();
            var currentBalance = await _context.SupplierLedgers
                .Where(l => l.SupplierId == request.SupplierId)
                .OrderByDescending(l => l.Id)
                .Select(l => l.Balance)
                .FirstOrDefaultAsync();

            // 5. Pagination
            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new SupplierLedgerPagedResultDto
            {
                SupplierName = supplier?.Name ?? "Unknown",
                CurrentBalance = currentBalance,
                Ledger = new PaginatedListDto<SupplierLedger>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                }
            };
        }

        public async Task<List<PendingDueDto>> GetPendingDuesAsync()
        {
            var latestEntries = await _context.SupplierLedgers
                .GroupBy(l => l.SupplierId)
                .Select(g => g.OrderByDescending(l => l.Id).FirstOrDefault())
                .ToListAsync();

            var dues = latestEntries
                .Where(l => l != null && l.Balance > 0)
                .Select(l => new { l.SupplierId, PendingAmount = l.Balance, ReferenceId = l.ReferenceId, l.TransactionDate })
                .ToList();

            var supplierIds = dues.Select(d => d.SupplierId).ToList();
            var suppliers = await _context.Suppliers.Where(s => supplierIds.Contains(s.Id)).ToListAsync();

            return dues.Select(d => new PendingDueDto
            {
                SupplierId = d.SupplierId,
                PendingAmount = d.PendingAmount,
                SupplierName = suppliers.FirstOrDefault(s => s.Id == d.SupplierId)?.Name ?? "Unknown",
                Status = (d.TransactionDate.AddDays(15) < System.DateTime.Now) ? "Overdue" : "Active",
                DueDate = d.TransactionDate.AddDays(15),
                LastReferenceId = d.ReferenceId
            }).ToList();
        }

        public async Task<decimal> GetTotalPaymentsAsync(DateRangeDto dateRange)
        {
            return await _context.SupplierPayments
                .Where(p => p.PaymentDate >= dateRange.StartDate && p.PaymentDate <= dateRange.EndDate)
                .SumAsync(p => p.Amount);
        }

        public async Task<Dictionary<string, decimal>> GetGRNPaymentStatusesAsync(List<string> grnNumbers)
        {
            if (grnNumbers == null || !grnNumbers.Any()) return new Dictionary<string, decimal>();

            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            // Fetch all payments that might be relevant. 
            var relevantPayments = await _context.SupplierLedgers
                .Where(l => l.TransactionType == "Payment" && l.Description != null)
                .Select(l => new { l.Description, l.ReferenceId, l.Debit })
                .ToListAsync();

            foreach (var grn in grnNumbers)
            {
                // Calculate total paid for this GRN
                decimal totalPaid = relevantPayments
                    .Where(p => 
                        (p.Description != null && p.Description.Contains(grn, StringComparison.OrdinalIgnoreCase)) ||
                        (p.ReferenceId != null && p.ReferenceId.Contains(grn, StringComparison.OrdinalIgnoreCase))
                    )
                    .Sum(p => p.Debit);

                result[grn] = totalPaid;
            }

            return result;
        }
        public async Task<PaginatedListDto<PaymentReportDto>> GetPaymentsReportAsync(PaymentReportRequestDto request)
        {
            var query = from p in _context.SupplierPayments
                        join s in _context.Suppliers on p.SupplierId equals s.Id
                        select new { p, s };

            // 1. Filtering by Date Range
            query = query.Where(x => x.p.PaymentDate >= request.StartDate && x.p.PaymentDate <= request.EndDate);

            // 2. Filtering by SupplierId
            if (request.SupplierId.HasValue && request.SupplierId.Value > 0)
            {
                query = query.Where(x => x.p.SupplierId == request.SupplierId.Value);
            }

            // 3. Searching (Global Search)
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

            // 4. Sorting
            query = request.SortBy.ToLower() switch
            {
                "paymentdate" => request.SortOrder == "desc" ? query.OrderByDescending(x => x.p.PaymentDate) : query.OrderBy(x => x.p.PaymentDate),
                "amount" => request.SortOrder == "desc" ? query.OrderByDescending(x => x.p.Amount) : query.OrderBy(x => x.p.Amount),
                "suppliername" => request.SortOrder == "desc" ? query.OrderByDescending(x => x.s.Name) : query.OrderBy(x => x.s.Name),
                "referencenumber" => request.SortOrder == "desc" ? query.OrderByDescending(x => x.p.ReferenceNumber) : query.OrderBy(x => x.p.ReferenceNumber),
                _ => query.OrderByDescending(x => x.p.PaymentDate)
            };

            // 5. Total Count
            var totalCount = await query.CountAsync();

            // 6. Pagination
            var pagedResults = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            // 7. Mapping to DTO
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
                CreatedBy = x.p.CreatedBy
            }).ToList();

            return new PaginatedListDto<PaymentReportDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        public async Task<decimal> GetTotalPendingDuesAsync()
        {
            var supplierIds = await _context.SupplierLedgers
                .Select(l => l.SupplierId)
                .Distinct()
                .ToListAsync();

            decimal total = 0;
            foreach (var id in supplierIds)
            {
                var lastBalance = await _context.SupplierLedgers
                    .Where(l => l.SupplierId == id)
                    .OrderByDescending(l => l.Id)
                    .Select(l => l.Balance)
                    .FirstOrDefaultAsync();

                // Only add positive balances (actual payables)
                if (lastBalance > 0)
                {
                    total += lastBalance;
                }
            }

            return total;
        }

        public async Task<Dictionary<int, decimal>> GetSupplierBalancesAsync(List<int> supplierIds)
        {
            if (supplierIds == null || !supplierIds.Any()) return new Dictionary<int, decimal>();

            // Get the LAST ledger entry for each supplier to get the current balance
            var latestBalances = await _context.SupplierLedgers
                .Where(l => supplierIds.Contains(l.SupplierId))
                .GroupBy(l => l.SupplierId)
                .Select(g => new
                {
                    SupplierId = g.Key,
                    Balance = g.OrderByDescending(x => x.Id).Select(x => x.Balance).FirstOrDefault()
                })
                .ToDictionaryAsync(x => x.SupplierId, x => x.Balance);

            return latestBalances;
        }

        public async Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(int months)
        {
            var startDate = System.DateTime.Now.AddMonths(-(months - 1));
            startDate = new System.DateTime(startDate.Year, startDate.Month, 1);

            var payments = await _context.SupplierPayments
                .Where(p => p.PaymentDate >= startDate)
                .ToListAsync();

            var trend = payments
                .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                .Select(g => new MonthlyTrendDto
                {
                    Month = new System.DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Amount = g.Sum(p => p.Amount)
                })
                .OrderBy(t => System.DateTime.Parse(t.Month))
                .ToList();

            return trend;
        }

        public async Task<bool> IsReferenceUniqueAsync(string referenceNumber)
        {
            if (string.IsNullOrWhiteSpace(referenceNumber)) return true; // Empty reference is allowed multiple times (e.g. simple cash)

            // Check in Payments
            bool existsInPayments = await _context.SupplierPayments.AnyAsync(r => r.ReferenceNumber == referenceNumber);
            if (existsInPayments) return false;

            // Check in Ledger
            bool existsInLedger = await _context.SupplierLedgers.AnyAsync(l => l.ReferenceId == referenceNumber);
            if (existsInLedger) return false;

            return true;
        }
    }
}
