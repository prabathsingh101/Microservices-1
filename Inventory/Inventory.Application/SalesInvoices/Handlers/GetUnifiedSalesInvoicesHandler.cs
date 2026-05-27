using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.SalesInvoices.DTOs;
using Inventory.Application.SalesInvoices.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Inventory.Application.Clients;
using System.Collections.Generic;

namespace Inventory.Application.SalesInvoices.Handlers
{
    public class GetUnifiedSalesInvoicesHandler : IRequestHandler<GetUnifiedSalesInvoicesQuery, UnifiedSalesPagedResultDto>
    {
        private readonly IInventoryDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICustomerClient _customerClient;

        public GetUnifiedSalesInvoicesHandler(
            IInventoryDbContext context,
            ICurrentUserService currentUserService,
            ICustomerClient customerClient)
        {
            _context = context;
            _currentUserService = currentUserService;
            _customerClient = customerClient;
        }

        public async Task<UnifiedSalesPagedResultDto> Handle(GetUnifiedSalesInvoicesQuery request, CancellationToken cancellationToken)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = string.IsNullOrEmpty(request.BranchId) ? _currentUserService.BranchId : request.BranchId;

            // 1. Projection for Quick Sales
            var quickSales = _context.SaleOrders
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
                .Select(x => new UnifiedSaleDto
                {
                    Id = x.Id,
                    InvoiceNo = x.SONumber,
                    Date = x.SODate,
                    CustomerId = x.CustomerId,
                    CustomerName = x.GuestName ?? string.Empty, // Will be filled by Client later if CustomerId exists
                    GrandTotal = x.GrandTotal,
                    TotalTax = x.TotalTax,
                    PaymentStatus = "Unpaid", // Default placeholder
                    Status = x.Status,
                    Source = "QuickSale",
                    CreatedBy = x.CreatedBy ?? string.Empty
                });

            // 2. Projection for Tax Invoices
            var taxSales = _context.SalesInvoices
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
                .Select(x => new UnifiedSaleDto
                {
                    Id = x.Id,
                    InvoiceNo = x.InvoiceNo,
                    Date = x.InvoiceDate,
                    CustomerId = x.CustomerId,
                    CustomerName = x.GuestName ?? string.Empty,
                    GrandTotal = x.GrandTotal,
                    TotalTax = x.TotalTax,
                    PaymentStatus = "Unpaid", // Default placeholder
                    Status = x.Status,
                    Source = "TaxInvoice",
                    CreatedBy = x.CreatedBy ?? string.Empty
                });

            // 3. UNION & Source Filter
            IQueryable<UnifiedSaleDto> combinedQuery;
            if (request.SourceFilter == "QuickSale")
            {
                combinedQuery = quickSales;
            }
            else if (request.SourceFilter == "TaxInvoice")
            {
                combinedQuery = taxSales;
            }
            else
            {
                combinedQuery = quickSales.Union(taxSales);
            }

            // 4. Filters
            if (request.StartDate.HasValue)
            {
                combinedQuery = combinedQuery.Where(o => o.Date >= request.StartDate.Value);
            }
            if (request.EndDate.HasValue)
            {
                var endOfDay = request.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                combinedQuery = combinedQuery.Where(o => o.Date <= endOfDay);
            }

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var search = request.SearchTerm.Trim().ToLower();
                var matchingCustomerIds = new List<Guid>();
                try
                {
                    matchingCustomerIds = await _customerClient.SearchCustomerIdsByNameAsync(search);
                }
                catch { /* Ignore */ }

                combinedQuery = combinedQuery.Where(o =>
                    o.InvoiceNo.ToLower().Contains(search) ||
                    o.Status.ToLower().Contains(search) ||
                    o.Source.ToLower().Contains(search) ||
                    o.GrandTotal.ToString().Contains(search) ||
                    (o.CustomerId.HasValue && matchingCustomerIds.Contains(o.CustomerId.Value)));
            }

            // 5. Total Count
            var totalCount = await combinedQuery.CountAsync(cancellationToken);

            // 6. Sorting
            bool isDesc = request.SortOrder?.ToLower() == "desc" || string.IsNullOrEmpty(request.SortOrder);
            var sortBy = (request.SortBy ?? "Date").ToLower();

            combinedQuery = sortBy switch
            {
                "invoiceno" => isDesc ? combinedQuery.OrderByDescending(x => x.InvoiceNo) : combinedQuery.OrderBy(x => x.InvoiceNo),
                "grandtotal" => isDesc ? combinedQuery.OrderByDescending(x => x.GrandTotal) : combinedQuery.OrderBy(x => x.GrandTotal),
                "status" => isDesc ? combinedQuery.OrderByDescending(x => x.Status) : combinedQuery.OrderBy(x => x.Status),
                "source" => isDesc ? combinedQuery.OrderByDescending(x => x.Source) : combinedQuery.OrderBy(x => x.Source),
                _ => isDesc ? combinedQuery.OrderByDescending(x => x.Date) : combinedQuery.OrderBy(x => x.Date)
            };

            // 7. Pagination
            var pagedData = await combinedQuery
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // 8. Populate Customer Names efficiently
            var customerIds = pagedData
                .Where(x => x.CustomerId.HasValue && x.CustomerId.Value != Guid.Empty)
                .Select(x => x.CustomerId!.Value)
                .Distinct()
                .ToList();

            var customerDictionary = await _customerClient.GetCustomerNamesAsync(customerIds);

            foreach (var item in pagedData)
            {
                if (item.CustomerId.HasValue && customerDictionary != null && customerDictionary.TryGetValue(item.CustomerId.Value, out var name))
                {
                    item.CustomerName = name;
                }
                else if (string.IsNullOrEmpty(item.CustomerName))
                {
                    item.CustomerName = "Cash Customer";
                }
            }

            return new UnifiedSalesPagedResultDto
            {
                Data = pagedData,
                TotalCount = totalCount
            };
        }
    }
}
