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
        private readonly Guid? _branchId;

        public FinanceRepository(CustomerDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _companyId = _currentUserService.CompanyId ?? Guid.Empty;
            _branchId = _currentUserService.BranchId;
        }

        public async Task AddReceiptAsync(CustomerReceipt receipt)
        {
            receipt.CompanyId = _companyId;
            if (receipt.BranchId == null || receipt.BranchId == Guid.Empty)
            {
                receipt.BranchId = _branchId;
            }
            await _context.CustomerReceipts.AddAsync(receipt);
        }

        public async Task<CustomerLedger?> GetLastLedgerEntryAsync(Guid customerId)
        {
            return await _context.CustomerLedgers
                .Where(l => l.CustomerId == customerId && l.CompanyId == _companyId && (l.BranchId == null || !_branchId.HasValue || l.BranchId == _branchId))
                .OrderByDescending(l => l.CreatedOn)
                .FirstOrDefaultAsync();
        }

        public async Task AddLedgerEntryAsync(CustomerLedger ledgerEntry)
        {
            ledgerEntry.CompanyId = _companyId;
            if (ledgerEntry.BranchId == null || ledgerEntry.BranchId == Guid.Empty)
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
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId && c.CompanyId == _companyId && (c.BranchId == null || !_branchId.HasValue || c.BranchId == _branchId));
            var query = _context.CustomerLedgers.Where(l => l.CustomerId == request.CustomerId && l.CompanyId == _companyId && (l.BranchId == null || !_branchId.HasValue || l.BranchId == _branchId));

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
                .Where(l => l.CustomerId == request.CustomerId && l.CompanyId == _companyId && (l.BranchId == null || !_branchId.HasValue || l.BranchId == _branchId))
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
                .Where(l => l.CompanyId == _companyId && (l.BranchId == null || !_branchId.HasValue || l.BranchId == _branchId))
                .Where(l => l.CreatedOn == _context.CustomerLedgers
                    .Where(inner => inner.CustomerId == l.CustomerId && inner.CompanyId == _companyId && (inner.BranchId == null || !_branchId.HasValue || inner.BranchId == _branchId))
                    .Max(inner => inner.CreatedOn))
                .Where(l => l.Balance > 0);

            var query = from l in latestEntries
                        join c in _context.Customers on l.CustomerId equals c.Id
                        where !internalAccountNames.Contains(c.CustomerName!) && (c.BranchId == null || !_branchId.HasValue || c.BranchId == _branchId)
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
            Guid? branchGuid = null;
            if (Guid.TryParse(dateRange.BranchId, out var bg)) branchGuid = bg;
            else branchGuid = _branchId;

            Guid finalCompanyId = _companyId;
            if (!string.IsNullOrEmpty(dateRange.CompanyId) && Guid.TryParse(dateRange.CompanyId, out var cg)) finalCompanyId = cg;

            return await _context.CustomerReceipts
                .Where(r => r.ReceiptDate >= dateRange.StartDate && r.ReceiptDate <= dateRange.EndDate && r.CompanyId == finalCompanyId && (r.BranchId == null || !branchGuid.HasValue || r.BranchId == branchGuid))
                .SumAsync(r => r.Amount);
        }

        public async Task<decimal> GetTotalOutstandingAsync(string? branchId = null, string? companyId = null)
        {
            Guid? finalBranchId = null;
            if (Guid.TryParse(branchId, out var bg)) finalBranchId = bg;
            else finalBranchId = _branchId;

            Guid finalCompanyId = _companyId;
            if (!string.IsNullOrEmpty(companyId) && Guid.TryParse(companyId, out var cg)) finalCompanyId = cg;

            var internalAccountNames = new[] { 
                "Proprietor (Self / Capital Account)", 
                "Company Bank Account (Internal)" 
            };
            
            var customerBalances = await _context.CustomerLedgers
                .Where(l => l.CompanyId == finalCompanyId && (l.BranchId == null || !finalBranchId.HasValue || l.BranchId == finalBranchId))
                .Where(l => l.CreatedOn == _context.CustomerLedgers
                    .Where(inner => inner.CustomerId == l.CustomerId && inner.CompanyId == finalCompanyId && (inner.BranchId == null || !finalBranchId.HasValue || inner.BranchId == finalBranchId))
                    .Max(inner => inner.CreatedOn))
                .Join(_context.Customers, l => l.CustomerId, c => c.Id, (l, c) => new { l, c })
                .Where(x => !internalAccountNames.Contains(x.c.CustomerName!) && x.c.CompanyId == finalCompanyId && (x.c.BranchId == null || !finalBranchId.HasValue || x.c.BranchId == finalBranchId))
                .Select(x => x.l.Balance)
                .ToListAsync();

            return customerBalances.Where(b => b > 0).Sum();
        }

        public async Task<List<OutstandingDto>> GetPendingDuesAsync(string? branchId = null, string? companyId = null)
        {
            Guid? finalBranchId = null;
            if (Guid.TryParse(branchId, out var bg)) finalBranchId = bg;
            else finalBranchId = _branchId;

            Guid finalCompanyId = _companyId;
            if (!string.IsNullOrEmpty(companyId) && Guid.TryParse(companyId, out var cg)) finalCompanyId = cg;

            var latestEntries = await _context.CustomerLedgers
                .Where(l => l.CompanyId == finalCompanyId && (l.BranchId == null || !finalBranchId.HasValue || l.BranchId == finalBranchId))
                .Where(l => l.CreatedOn == _context.CustomerLedgers
                    .Where(inner => inner.CustomerId == l.CustomerId && inner.CompanyId == finalCompanyId && (inner.BranchId == null || !finalBranchId.HasValue || inner.BranchId == finalBranchId))
                    .Max(inner => inner.CreatedOn))
                .Where(l => l.Balance > 0)
                .ToListAsync();

            var customerIds = latestEntries.Select(d => d.CustomerId).ToList();
            var customers = await _context.Customers.Where(c => customerIds.Contains(c.Id) && (c.BranchId == null || !finalBranchId.HasValue || c.BranchId == finalBranchId)).ToListAsync();

            return latestEntries.Select(l => {
                var c = customers.FirstOrDefault(c => c.Id == l.CustomerId);
                return new OutstandingDto
                {
                    CustomerId = l.CustomerId,
                    CustomerName = c?.CustomerName,
                    Phone = c?.Phone,
                    PendingAmount = l.Balance,
                    TotalAmount = l.Balance,
                    Status = (l.TransactionDate.AddDays(15) < DateTime.Now) ? "Overdue" : "Active",
                    DueDate = l.TransactionDate.AddDays(15)
                };
            }).ToList();
        }

        public async Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(int months, string? branchId = null, string? companyId = null)
        {
            Guid? finalBranchId = null;
            if (Guid.TryParse(branchId, out var bg)) finalBranchId = bg;
            else finalBranchId = _branchId;

            Guid finalCompanyId = _companyId;
            if (!string.IsNullOrEmpty(companyId) && Guid.TryParse(companyId, out var cg)) finalCompanyId = cg;

            var startDate = DateTime.Now.AddMonths(-(months - 1));
            startDate = new DateTime(startDate.Year, startDate.Month, 1);

            var receipts = await _context.CustomerReceipts
                .Where(r => r.ReceiptDate >= startDate && r.CompanyId == finalCompanyId && (r.BranchId == null || !finalBranchId.HasValue || r.BranchId == finalBranchId))
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
            
            bool existsInReceipts = await _context.CustomerReceipts.AnyAsync(r => r.ReferenceNumber == referenceNumber && r.CompanyId == _companyId && (r.BranchId == null || !_branchId.HasValue || r.BranchId == _branchId));
            if (existsInReceipts) return (false, "Receipts");

            bool existsInLedger = await _context.CustomerLedgers.AnyAsync(l => l.ReferenceId == referenceNumber && l.CompanyId == _companyId && (l.BranchId == null || !_branchId.HasValue || l.BranchId == _branchId));
            if (existsInLedger) return (false, "Customer Ledgers");

            return (true, string.Empty);
        }

        public async Task<PaginatedListDto<ReceiptReportDto>> GetReceiptsReportAsync(ReceiptReportRequestDto request)
        {
            Guid? branchGuid = null;
            if (Guid.TryParse(request.BranchId, out var bg)) branchGuid = bg;
            else branchGuid = _branchId;

            var query = _context.CustomerReceipts.Where(r => r.CompanyId == _companyId && (r.BranchId == null || !branchGuid.HasValue || r.BranchId == branchGuid)).AsQueryable();

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
            var customers = await _context.Customers.Where(c => customerIds.Contains(c.Id) && (c.BranchId == null || !_branchId.HasValue || c.BranchId == _branchId)).ToDictionaryAsync(c => c.Id, c => c.CustomerName);

            var items = pagedResults.Select(r => new ReceiptReportDto
            {
                Id = r.Id,
                CustomerId = r.CustomerId,
                CustomerName = customers.GetValueOrDefault(r.CustomerId) ?? "Unknown",
                Amount = r.Amount,
                ReceiptDate = r.ReceiptDate,
                ReceiptMode = r.ReceiptMode,
                ReferenceNumber = r.ReferenceNumber,
                Remarks = r.Remarks,
                CreatedBy = r.CreatedBy
            }).ToList();

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
