using Customers.Application.Common.Interfaces;
using Customers.Application.DTOs;
using Customers.Domain.Entities;
using Customers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Customers.Infrastructure.Repositories
{
    public class FinanceRepository : IFinanceRepository
    {
        private readonly CustomerDbContext _context;

        public FinanceRepository(CustomerDbContext context)
        {
            _context = context;
        }

        public async Task AddReceiptAsync(CustomerReceipt receipt)
        {
            await _context.CustomerReceipts.AddAsync(receipt);
        }

        public async Task<CustomerLedger?> GetLastLedgerEntryAsync(int customerId)
        {
            return await _context.CustomerLedgers
                .Where(l => l.CustomerId == customerId)
                .OrderByDescending(l => l.Id)
                .FirstOrDefaultAsync();
        }

        public async Task AddLedgerEntryAsync(CustomerLedger ledgerEntry)
        {
            await _context.CustomerLedgers.AddAsync(ledgerEntry);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<CustomerLedgerPagedResultDto> GetLedgerAsync(CustomerLedgerRequestDto request)
        {
            var customer = await _context.Customers.FindAsync(request.CustomerId);
            var query = _context.CustomerLedgers.Where(l => l.CustomerId == request.CustomerId);

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
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
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
            var currentBalance = await _context.CustomerLedgers
                .Where(l => l.CustomerId == request.CustomerId)
                .OrderByDescending(l => l.Id)
                .Select(l => l.Balance)
                .FirstOrDefaultAsync();

            // 5. Pagination
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
            // 1. Get latest ledger entry for each customer
            var latestLedgerIds = _context.CustomerLedgers
                .GroupBy(l => l.CustomerId)
                .Select(g => g.Max(l => l.Id));

            var query = from l in _context.CustomerLedgers
                        join c in _context.Customers on l.CustomerId equals c.Id
                        where latestLedgerIds.Contains(l.Id) && l.Balance > 0
                        select new OutstandingDto
                        {
                            CustomerId = l.CustomerId,
                            CustomerName = c.CustomerName,
                            PendingAmount = l.Balance,
                            TotalAmount = l.Balance, 
                            Status = (l.TransactionDate.AddDays(15) < System.DateTime.Now) ? "Overdue" : "Active",
                            DueDate = l.TransactionDate.AddDays(15), 
                            LastReferenceId = l.ReferenceId
                        };

            // 2. Searching
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(o => o.CustomerName.ToLower().Contains(term) || o.CustomerId.ToString().Contains(term));
            }

            // 2.1 Customer Name Filtering
            if (!string.IsNullOrWhiteSpace(request.CustomerNameFilter))
            {
                var name = request.CustomerNameFilter.ToLower();
                query = query.Where(o => o.CustomerName.ToLower().Contains(name));
            }

            // 2.2 Status Filtering
            if (!string.IsNullOrWhiteSpace(request.StatusFilter))
            {
                var status = request.StatusFilter.ToLower();
                query = query.Where(o => o.Status.ToLower().Contains(status));
            }

            // 3. Sorting
            query = request.SortBy.ToLower() switch
            {
                "customerid" => request.SortOrder == "desc" ? query.OrderByDescending(o => o.CustomerId) : query.OrderBy(o => o.CustomerId),
                "customername" => request.SortOrder == "desc" ? query.OrderByDescending(o => o.CustomerName) : query.OrderBy(o => o.CustomerName),
                "pendingamount" => request.SortOrder == "desc" ? query.OrderByDescending(o => o.PendingAmount) : query.OrderBy(o => o.PendingAmount),
                "totalamount" => request.SortOrder == "desc" ? query.OrderByDescending(o => o.TotalAmount) : query.OrderBy(o => o.TotalAmount),
                "status" => request.SortOrder == "desc" ? query.OrderByDescending(o => o.Status) : query.OrderBy(o => o.Status),
                "duedate" => request.SortOrder == "desc" ? query.OrderByDescending(o => o.DueDate) : query.OrderBy(o => o.DueDate),
                _ => request.SortOrder == "desc" ? query.OrderByDescending(o => o.CustomerName) : query.OrderBy(o => o.CustomerName)
            };

            // 4. Counts and Totals
            var totalCount = await query.CountAsync();
            var totalAmount = await query.SumAsync(o => o.PendingAmount);

            // 5. Pagination
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
            return await _context.CustomerReceipts
                .Where(r => r.ReceiptDate >= dateRange.StartDate && r.ReceiptDate <= dateRange.EndDate)
                .SumAsync(r => r.Amount);
        }

        public async Task<decimal> GetTotalOutstandingAsync()
        {
            // Get all REAL customer IDs (Exclude internal Capital/Owner Accounts)
            var internalAccountNames = new[] { "Proprietor (Self / Capital Account)" };
            
            var customerIds = await _context.Customers
                .Where(c => !internalAccountNames.Contains(c.CustomerName))
                .Select(c => c.Id)
                .ToListAsync();

            decimal total = 0;
            foreach (var id in customerIds)
            {
                var lastBalance = await _context.CustomerLedgers
                    .Where(l => l.CustomerId == id)
                    .OrderByDescending(l => l.Id)
                    .Select(l => l.Balance)
                    .FirstOrDefaultAsync();
                
                total += lastBalance;
            }

            return total;
        }

        public async Task<List<OutstandingDto>> GetPendingDuesAsync()
        {
            // 1. Get latest ledger entry ID for each customer
            var latestLedgerIds = await _context.CustomerLedgers
                .GroupBy(l => l.CustomerId)
                .Select(g => g.Max(l => l.Id))
                .ToListAsync();

            // 2. Fetch those ledger entries and join with Customers (Phone bhi include karein)
            var dues = await (from l in _context.CustomerLedgers
                        join c in _context.Customers on l.CustomerId equals c.Id
                        where latestLedgerIds.Contains(l.Id) && l.Balance > 0
                        select new OutstandingDto
                        {
                            CustomerId = l.CustomerId,
                            CustomerName = c.CustomerName,
                            Phone = c.Phone,  // Customer ka WhatsApp number
                            PendingAmount = l.Balance,
                            TotalAmount = l.Balance,
                            Status = (l.TransactionDate.AddDays(15) < System.DateTime.Now) ? "Overdue" : "Active",
                            DueDate = l.TransactionDate.AddDays(15)
                        }).ToListAsync();

            return dues;
        }

        public async Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(int months)
        {
            var startDate = System.DateTime.Now.AddMonths(-(months - 1));
            startDate = new System.DateTime(startDate.Year, startDate.Month, 1);

            var receipts = await _context.CustomerReceipts
                .Where(r => r.ReceiptDate >= startDate)
                .ToListAsync();

            var trend = receipts
                .GroupBy(r => new { r.ReceiptDate.Year, r.ReceiptDate.Month })
                .Select(g => new MonthlyTrendDto
                {
                    Month = new System.DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Amount = g.Sum(r => r.Amount)
                })
                .OrderBy(t => System.DateTime.Parse(t.Month))
                .ToList();

            return trend;
        }

        public async Task<bool> IsReferenceUniqueAsync(string referenceNumber)
        {
            if (string.IsNullOrWhiteSpace(referenceNumber)) return true; // Empty reference is allowed multiple times (e.g. simple cash)
            
            // Check in Receipts
            bool existsInReceipts = await _context.CustomerReceipts.AnyAsync(r => r.ReferenceNumber == referenceNumber);
            if (existsInReceipts) return false;

            // Check in Ledger (for safety)
            bool existsInLedger = await _context.CustomerLedgers.AnyAsync(l => l.ReferenceId == referenceNumber);
            if (existsInLedger) return false;

            return true;
        }

        public async Task<PaginatedListDto<ReceiptReportDto>> GetReceiptsReportAsync(ReceiptReportRequestDto request)
        {
            var query = from r in _context.CustomerReceipts
                        join c in _context.Customers on r.CustomerId equals c.Id
                        select new ReceiptReportDto
                        {
                            Id = r.Id,
                            CustomerId = r.CustomerId,
                            CustomerName = c.CustomerName,
                            Amount = r.Amount,
                            ReceiptDate = r.ReceiptDate,
                            ReceiptMode = r.ReceiptMode,
                            ReferenceNumber = r.ReferenceNumber,
                            Remarks = r.Remarks,
                            CreatedBy = r.CreatedBy
                        };

            // 1. Filtering
            query = query.Where(r => r.ReceiptDate >= request.StartDate && r.ReceiptDate <= request.EndDate);

            if (request.CustomerId.HasValue)
                query = query.Where(r => r.CustomerId == request.CustomerId.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(r => 
                    r.CustomerName.ToLower().Contains(term) || 
                    (r.ReferenceNumber != null && r.ReferenceNumber.ToLower().Contains(term)) || 
                    (r.Remarks != null && r.Remarks.ToLower().Contains(term))
                );
            }

            // 2. Sorting
            query = request.SortBy.ToLower() switch
            {
                "receiptdate" => request.SortOrder == "desc" ? query.OrderByDescending(r => r.ReceiptDate) : query.OrderBy(r => r.ReceiptDate),
                "customername" => request.SortOrder == "desc" ? query.OrderByDescending(r => r.CustomerName) : query.OrderBy(r => r.CustomerName),
                "amount" => request.SortOrder == "desc" ? query.OrderByDescending(r => r.Amount) : query.OrderBy(r => r.Amount),
                "receiptmode" => request.SortOrder == "desc" ? query.OrderByDescending(r => r.ReceiptMode) : query.OrderBy(r => r.ReceiptMode),
                _ => query.OrderByDescending(r => r.ReceiptDate)
            };

            // 3. Counts and Pagination
            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PaginatedListDto<ReceiptReportDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
    }
}
