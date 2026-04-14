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

        public FinanceRepository(CustomerDbContext context)
        {
            _context = context;
        }

        public async Task AddReceiptAsync(CustomerReceipt receipt)
        {
            await _context.CustomerReceipts.AddAsync(receipt);
        }

        public async Task<CustomerLedger?> GetLastLedgerEntryAsync(Guid customerId)
        {
            return await _context.CustomerLedgers
                .Where(l => l.CustomerId == customerId)
                .OrderByDescending(l => l.CreatedOn)
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
                .Where(l => l.CustomerId == request.CustomerId)
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
                .GroupBy(l => l.CustomerId)
                .Select(g => new { 
                    CustomerId = g.Key, 
                    LastEntry = g.OrderByDescending(x => x.CreatedOn).FirstOrDefault() 
                })
                .Where(x => x.LastEntry != null && x.LastEntry.Balance > 0);

            var query = from x in latestEntries
                        join c in _context.Customers on x.CustomerId equals c.Id
                        where !internalAccountNames.Contains(c.CustomerName!)
                        select new OutstandingDto
                        {
                            CustomerId = x.CustomerId,
                            CustomerName = c.CustomerName,
                            PendingAmount = x.LastEntry!.Balance,
                            TotalAmount = x.LastEntry.Balance, 
                            Status = (x.LastEntry.TransactionDate.AddDays(15) < DateTime.Now) ? "Overdue" : "Active",
                            DueDate = x.LastEntry.TransactionDate.AddDays(15), 
                            LastReferenceId = x.LastEntry.ReferenceId
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
            return await _context.CustomerReceipts
                .Where(r => r.ReceiptDate >= dateRange.StartDate && r.ReceiptDate <= dateRange.EndDate)
                .SumAsync(r => r.Amount);
        }

        public async Task<decimal> GetTotalOutstandingAsync()
        {
            var internalAccountNames = new[] { 
                "Proprietor (Self / Capital Account)", 
                "Company Bank Account (Internal)" 
            };
            
            var customerBalances = await _context.CustomerLedgers
                .Join(_context.Customers, l => l.CustomerId, c => c.Id, (l, c) => new { l, c })
                .Where(x => !internalAccountNames.Contains(x.c.CustomerName!))
                .GroupBy(x => x.l.CustomerId)
                .Select(g => g.OrderByDescending(x => x.l.CreatedOn).Select(x => x.l.Balance).FirstOrDefault())
                .ToListAsync();

            return customerBalances.Where(b => b > 0).Sum();
        }

        public async Task<List<OutstandingDto>> GetPendingDuesAsync()
        {
            var latestEntries = await _context.CustomerLedgers
                .GroupBy(l => l.CustomerId)
                .Select(g => new { 
                    CustomerId = g.Key, 
                    LastEntry = g.OrderByDescending(x => x.CreatedOn).FirstOrDefault() 
                })
                .Where(x => x.LastEntry != null && x.LastEntry.Balance > 0)
                .ToListAsync();

            var customerIds = latestEntries.Select(d => d.CustomerId).ToList();
            var customers = await _context.Customers.Where(c => customerIds.Contains(c.Id)).ToListAsync();

            return latestEntries.Select(d => {
                var c = customers.FirstOrDefault(c => c.Id == d.CustomerId);
                return new OutstandingDto
                {
                    CustomerId = d.CustomerId,
                    CustomerName = c?.CustomerName,
                    Phone = c?.Phone,
                    PendingAmount = d.LastEntry!.Balance,
                    TotalAmount = d.LastEntry.Balance,
                    Status = (d.LastEntry.TransactionDate.AddDays(15) < DateTime.Now) ? "Overdue" : "Active",
                    DueDate = d.LastEntry.TransactionDate.AddDays(15)
                };
            }).ToList();
        }

        public async Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(int months)
        {
            var startDate = DateTime.Now.AddMonths(-(months - 1));
            startDate = new DateTime(startDate.Year, startDate.Month, 1);

            var receipts = await _context.CustomerReceipts
                .Where(r => r.ReceiptDate >= startDate)
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
            if (string.IsNullOrWhiteSpace(referenceNumber)) return true;
            
            bool existsInReceipts = await _context.CustomerReceipts.AnyAsync(r => r.ReferenceNumber == referenceNumber);
            if (existsInReceipts) return false;

            return await _context.CustomerLedgers.AnyAsync(l => l.ReferenceId == referenceNumber);
        }

        public async Task<PaginatedListDto<ReceiptReportDto>> GetReceiptsReportAsync(ReceiptReportRequestDto request)
        {
            var query = _context.CustomerReceipts.AsQueryable();

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
            var customers = await _context.Customers.Where(c => customerIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.CustomerName);

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
