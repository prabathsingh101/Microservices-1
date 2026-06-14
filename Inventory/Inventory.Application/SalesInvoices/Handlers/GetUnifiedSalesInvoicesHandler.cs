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
                    CreatedBy = x.CreatedBy ?? string.Empty,
                    DeliveryChallanId = null,
                    ChallanNo = null,
                    IsQuick = x.IsQuick,
                    TotalQty = x.Items.Sum(i => i.Qty),
                    GatePassNo = x.GatePassNo,
                    DoctorName = x.DoctorName,
                    DoctorRegNo = x.DoctorRegNo
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
                    CreatedBy = x.CreatedBy ?? string.Empty,
                    DeliveryChallanId = x.DeliveryChallanId,
                    ChallanNo = _context.DeliveryChallans
                        .Where(dc => dc.Id == x.DeliveryChallanId)
                        .Select(dc => dc.ChallanNo)
                        .FirstOrDefault(),
                    IsQuick = x.IsQuick,
                    TotalQty = x.Items.Sum(i => i.Qty),
                    GatePassNo = null,
                    DoctorName = x.DoctorName,
                    DoctorRegNo = x.DoctorRegNo
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
            if (request.IsQuick.HasValue)
            {
                combinedQuery = combinedQuery.Where(o => o.IsQuick == request.IsQuick.Value);
            }

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

            // 5. Total Count & Stats
            var totalCount = await combinedQuery.CountAsync(cancellationToken);

            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var tomorrow = today.AddDays(1);

            var totalSalesAmount = await combinedQuery.Where(o => o.Status == "Confirmed").SumAsync(o => (decimal?)o.GrandTotal, cancellationToken) ?? 0;
            var todayCount = await combinedQuery.Where(o => o.Date >= today && o.Date < tomorrow).CountAsync(cancellationToken);
            var monthCount = await combinedQuery.Where(o => o.Date >= monthStart).CountAsync(cancellationToken);

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

            // 8. Populate Customer Names efficiently & Challan Numbers for Consolidated Invoices
            var customerIds = pagedData
                .Where(x => x.CustomerId.HasValue && x.CustomerId.Value != Guid.Empty)
                .Select(x => x.CustomerId!.Value)
                .Distinct()
                .ToList();

            var customerDictionary = await _customerClient.GetCustomerNamesAsync(customerIds);

            var invoiceIds = pagedData.Where(x => x.Source == "TaxInvoice").Select(x => x.Id).ToList();
            var relations = await _context.SalesInvoiceDeliveryChallans
                .AsNoTracking()
                .Where(r => invoiceIds.Contains(r.SalesInvoiceId))
                .Select(r => new { r.SalesInvoiceId, r.DeliveryChallanId, ChallanNo = r.DeliveryChallan != null ? r.DeliveryChallan.ChallanNo : string.Empty })
                .ToListAsync(cancellationToken);

            var relationGroup = relations.GroupBy(r => r.SalesInvoiceId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Get returned quantities to calculate if the order is fully returned
            var saleOrderIds = pagedData.Select(x => x.Id).ToList();
            var returnedQuantities = await _context.SaleReturnItems
                .Where(ri => saleOrderIds.Contains(ri.SaleReturnHeader.SaleOrderId) && 
                             (ri.SaleReturnHeader.Status == "Confirmed" || ri.SaleReturnHeader.Status == "INWARDED" || ri.SaleReturnHeader.Status == "Refunded" || ri.SaleReturnHeader.Status == "Exchanged"))
                .GroupBy(ri => ri.SaleReturnHeader.SaleOrderId)
                .Select(g => new { SaleOrderId = g.Key, TotalReturned = g.Sum(ri => ri.ReturnQty) })
                .ToDictionaryAsync(x => x.SaleOrderId, x => x.TotalReturned, cancellationToken);

            foreach (var item in pagedData)
            {
                // Customer Name
                if (item.CustomerId.HasValue && customerDictionary != null && customerDictionary.TryGetValue(item.CustomerId.Value, out var name))
                {
                    item.CustomerName = name;
                }
                else if (string.IsNullOrEmpty(item.CustomerName))
                {
                    item.CustomerName = "Cash Customer";
                }

                // Consolidated Challan numbers
                if (item.Source == "TaxInvoice" && relationGroup.TryGetValue(item.Id, out var linkedChallans) && linkedChallans.Any())
                {
                    item.ChallanNo = string.Join(", ", linkedChallans.Select(c => c.ChallanNo).Where(c => !string.IsNullOrEmpty(c)));
                    item.DeliveryChallanId = linkedChallans.First().DeliveryChallanId;
                }

                // Set IsReturnable
                var returnedQty = returnedQuantities.ContainsKey(item.Id) ? returnedQuantities[item.Id] : 0;
                item.IsReturnable = returnedQty < item.TotalQty;
            }

            return new UnifiedSalesPagedResultDto
            {
                Data = pagedData,
                TotalCount = totalCount,
                TotalSalesAmount = totalSalesAmount,
                TodayCount = todayCount,
                MonthCount = monthCount
            };
        }
    }
}
