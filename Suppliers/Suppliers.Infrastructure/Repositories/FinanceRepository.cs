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
    public class FinanceRepository(SupplierDbContext context) : IFinanceRepository
    {
        private readonly SupplierDbContext _context = context;

        public async Task AddPaymentAsync(SupplierPayment payment)
        {
            await _context.SupplierPayments.AddAsync(payment);
        }

        public async Task<SupplierLedger?> GetLastLedgerEntryAsync(Guid supplierId)
        {
            return await _context.SupplierLedgers
                .Where(l => l.SupplierId == supplierId)
                .OrderByDescending(l => l.CreatedOn)
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
            var currentBalance = await _context.SupplierLedgers
                .Where(l => l.SupplierId == request.SupplierId)
                .OrderByDescending(l => l.CreatedOn)
                .Select(l => l.Balance)
                .FirstOrDefaultAsync();

            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

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

        public async Task<List<PendingDueDto>> GetPendingDuesAsync()
        {
            var suppliersWithBalances = await _context.SupplierLedgers
                .GroupBy(l => l.SupplierId)
                .Select(g => new { 
                    SupplierId = g.Key, 
                    LastEntry = g.OrderByDescending(x => x.CreatedOn).FirstOrDefault() 
                })
                .Where(x => x.LastEntry != null && x.LastEntry.Balance > 0)
                .ToListAsync();

            var supplierIds = suppliersWithBalances.Select(d => d.SupplierId).ToList();
            var suppliers = await _context.Suppliers.Where(s => supplierIds.Contains(s.Id)).ToListAsync();

            return suppliersWithBalances.Select(d => new PendingDueDto
            {
                SupplierId = d.SupplierId,
                PendingAmount = d.LastEntry!.Balance,
                SupplierName = suppliers.FirstOrDefault(s => s.Id == d.SupplierId)?.Name ?? "Unknown",
                Status = (d.LastEntry.TransactionDate.AddDays(15) < DateTime.Now) ? "Overdue" : "Active",
                DueDate = d.LastEntry.TransactionDate.AddDays(15),
                LastReferenceId = d.LastEntry.ReferenceId
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

            var relevantPayments = await _context.SupplierLedgers
                .Where(l => l.TransactionType == "Payment" && l.Description != null)
                .Select(l => new { l.Description, l.ReferenceId, l.Debit })
                .ToListAsync();

            foreach (var grn in grnNumbers)
            {
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
            var supplierBalances = await _context.SupplierLedgers
                .GroupBy(l => l.SupplierId)
                .Select(g => g.OrderByDescending(x => x.CreatedOn).Select(x => x.Balance).FirstOrDefault())
                .ToListAsync();

            return supplierBalances.Where(b => b > 0).Sum();
        }

        public async Task<Dictionary<Guid, decimal>> GetSupplierBalancesAsync(List<Guid> supplierIds)
        {
            if (supplierIds == null || !supplierIds.Any()) return new Dictionary<Guid, decimal>();

            return await _context.SupplierLedgers
                .Where(l => supplierIds.Contains(l.SupplierId))
                .GroupBy(l => l.SupplierId)
                .Select(g => new
                {
                    SupplierId = g.Key,
                    Balance = g.OrderByDescending(x => x.CreatedOn).Select(x => x.Balance).FirstOrDefault()
                })
                .ToDictionaryAsync(x => x.SupplierId, x => x.Balance);
        }

        public async Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(int months)
        {
            var startDate = DateTime.Now.AddMonths(-(months - 1));
            startDate = new DateTime(startDate.Year, startDate.Month, 1);

            var payments = await _context.SupplierPayments
                .Where(p => p.PaymentDate >= startDate)
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

        public async Task<bool> IsReferenceUniqueAsync(string referenceNumber)
        {
            if (string.IsNullOrWhiteSpace(referenceNumber)) return true;

            bool existsInPayments = await _context.SupplierPayments.AnyAsync(r => r.ReferenceNumber == referenceNumber);
            if (existsInPayments) return false;

            return await _context.SupplierLedgers.AnyAsync(l => l.ReferenceId == referenceNumber);
        }
    }
}
