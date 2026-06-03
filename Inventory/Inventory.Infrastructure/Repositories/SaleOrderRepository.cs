using Inventory.Application.Common.Interfaces;
using Inventory.Application.DTOs.SaleOrder;
using Inventory.Application.SaleOrders.DTOs;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System;
using System.Collections.Generic;
using Inventory.Application.Clients;
using Inventory.Domain.Entities.SO;
using Inventory.Domain.Entities;

public class SaleOrderRepository : ISaleOrderRepository
{
    private readonly ICurrentUserService _currentUserService;
    private readonly InventoryDbContext _context;
    private IDbContextTransaction? _transaction;
    private readonly ICustomerClient _customerClient;
    private readonly ICompanyClient _companyClient;

    public SaleOrderRepository(InventoryDbContext context, ICustomerClient customerClient, ICompanyClient companyClient, ICurrentUserService currentUserService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _customerClient = customerClient;
        _companyClient = companyClient;
        _currentUserService = currentUserService;
    }

    // BeginTransactionAsync logic fix
    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task ExecuteInTransactionAsync(Func<Task> action)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await action();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    // Guid aur Decimal support ke saath methods
    public async Task<decimal> GetAvailableStockAsync(Guid productId)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        
        // ✅ Read directly from WarehouseStocks — single source of truth
        // WarehouseStocks is updated on every GRN, PurchaseReturn, Sale, and SaleReturn event
        var totalStock = await _context.WarehouseStocks
            .Where(ws => ws.ProductId == productId && ws.CompanyId == companyId)
            .SumAsync(ws => (decimal?)ws.Quantity) ?? 0;

        return totalStock;
    }

    // REMOVED: UpdateProductStockAsync (Now using Live Transactions)


    public async Task<string> GetLastSONumberAsync()
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        return await _context.SaleOrders
            .Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
            .OrderByDescending(x => x.SODate) // SODate se sort karna behtar hai [cite: 2026-04-19]
            .ThenByDescending(x => x.SONumber)
            .Select(x => x.SONumber)
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> SaveAsync(SaleOrder order)
    {
        _context.SaleOrders.Add(order);
        await _context.SaveChangesAsync();
        return order.Id;
    }

    public async Task UpdateAsync(SaleOrder order)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        var existingOrder = await _context.SaleOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == order.Id && o.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || o.BranchId == branchId));

        if (existingOrder != null)
        {
            // Parent properties update
            existingOrder.CustomerId = order.CustomerId;
            existingOrder.SODate = order.SODate;
            existingOrder.ExpectedDeliveryDate = order.ExpectedDeliveryDate;
            existingOrder.SubTotal = order.SubTotal;
            existingOrder.TotalTax = order.TotalTax;
            existingOrder.GrandTotal = order.GrandTotal;
            existingOrder.Remarks = order.Remarks;
            existingOrder.Status = order.Status;
            existingOrder.TaxType = order.TaxType;
            existingOrder.TdsPercent = order.TdsPercent;
            existingOrder.TdsAmount = order.TdsAmount;
            existingOrder.TcsPercent = order.TcsPercent;
            existingOrder.TcsAmount = order.TcsAmount;
            existingOrder.IgstAmount = order.IgstAmount;
            existingOrder.CgstAmount = order.CgstAmount;
            existingOrder.SgstAmount = order.SgstAmount;
            existingOrder.GuestName = order.GuestName;
            existingOrder.GuestPhone = order.GuestPhone;

            // Remove old items and add new ones (Sync)
            _context.SaleOrderItems.RemoveRange(existingOrder.Items);
            foreach (var item in order.Items)
            {
                existingOrder.Items.Add(item);
            }

            _context.SaleOrders.Update(existingOrder);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        var order = await _context.SaleOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id && o.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || o.BranchId == branchId));
        if (order == null) return false;

        _context.SaleOrderItems.RemoveRange(order.Items);
        _context.SaleOrders.Remove(order);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<List<StockExportDto>> GetSaleReportDataAsync(List<Guid> orderIds)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        // Selected Orders ke Product IDs fetch karein
        return await _context.SaleOrderItems
            .Where(si => orderIds.Contains(si.SaleOrderId) && si.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || si.BranchId == branchId)) // Filter by selected Guid IDs
            .GroupBy(si => new { si.ProductId, si.ProductName, si.Unit })
            .Select(group => new StockExportDto
            {
                ProductName = group.Key.ProductName,
                Unit = group.Key.Unit,
                TotalReceived = _context.GRNDetails.Where(g => g.ProductId == group.Key.ProductId && g.CompanyId == companyId).Sum(x => x.ReceivedQty),
                TotalRejected = _context.GRNDetails.Where(g => g.ProductId == group.Key.ProductId && g.CompanyId == companyId).Sum(x => x.RejectedQty),
                AvailableStock = (_context.GRNDetails.Where(g => g.ProductId == group.Key.ProductId && g.CompanyId == companyId).Sum(x => x.ReceivedQty) -
                                  _context.GRNDetails.Where(g => g.ProductId == group.Key.ProductId && g.CompanyId == companyId).Sum(x => x.RejectedQty)) -
                                 (_context.SaleOrderItems.Where(si => si.ProductId == group.Key.ProductId && (si.SaleOrder.Status == "Confirmed" || si.SaleOrder.Status == "Delivered" || si.SaleOrder.Status == "Completed") && si.CompanyId == companyId).Sum(si => (decimal?)si.Qty) ?? 0)
            }).ToListAsync();
    }

    public async Task<(List<SaleOrderListDto> Data, int TotalCount, decimal TotalSalesAmount, int PendingDispatchCount, int UnpaidOrdersCount, int TodayCount, int MonthCount)> GetAllSaleOrdersAsync(
     string searchTerm,
     int pageNumber,
     int pageSize,
     string sortBy,
     string sortOrder,
     bool isQuick = false,
     DateTime? startDate = null,
     DateTime? endDate = null,
     string? branchId = null)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var finalBranchId = !string.IsNullOrEmpty(branchId) ? branchId : _currentUserService.BranchId;

        // 1. Optimized Base Query
        var query = _context.SaleOrders
            .AsNoTracking()
            .Where(o => o.CompanyId == companyId && o.IsQuick == isQuick && (string.IsNullOrEmpty(finalBranchId) || o.BranchId == finalBranchId))
            .AsQueryable();

        // 2. Date Range Filter
        if (startDate.HasValue)
        {
            query = query.Where(o => o.SODate >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(o => o.SODate <= endOfDay);
        }

        // 2. Searching logic [cite: 2026-02-03]
        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.Trim().ToLower();

            // Fetch matching customer IDs from External Service
            var matchingCustomerIds = new List<Guid>();
            try
            {
                matchingCustomerIds = await _customerClient.SearchCustomerIdsByNameAsync(searchTerm);
            }
            catch { /* Ignore microservice failure for search */ }

            query = query.Where(o =>
                o.SONumber.ToLower().Contains(searchTerm) ||
                (o.GatePassNo != null && o.GatePassNo.ToLower().Contains(searchTerm)) ||
                o.Status.ToLower().Contains(searchTerm) ||
                o.GrandTotal.ToString().Contains(searchTerm) ||
                (o.CustomerId.HasValue && matchingCustomerIds.Contains(o.CustomerId.Value)));
        }

        // 🎯 3. Calculate Global Stats (Before Pagination)
        var totalCount = await query.CountAsync();
        var totalSalesAmount = await query.Where(o => o.Status == "Confirmed").SumAsync(o => (decimal?)o.GrandTotal) ?? 0;
        var pendingDispatchCount = await query.Where(o => o.Status == "Confirmed" && (o.GatePassNo == null || o.GatePassNo == "")).CountAsync();

        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var tomorrow = today.AddDays(1);

        var todayCount = await query.Where(o => o.SODate >= today && o.SODate < tomorrow).CountAsync();
        var monthCount = await query.Where(o => o.SODate >= monthStart).CountAsync();

        // Note: Unpaid count is hard to calculate globally without Finance Service data 
        var unpaidOrdersCount = 0;

        // 4. Enhanced Sorting Logic
        bool isDesc = sortOrder?.ToLower() == "desc" || string.IsNullOrEmpty(sortOrder);
        string sortProperty = (sortBy ?? "").ToLower().Trim() switch
        {
            "sonumber" => "SONumber",
            "sodate" or "date" => "SODate",
            "status" => "Status",
            "grandtotal" or "amount" => "GrandTotal",
            "createdon" or "createddate" => "CreatedOn",
            "id" => "Id",
            _ => "CreatedOn" // Default order by CreatedOn desc
        };

        if (isDesc)
            query = query.OrderByDescending(o => EF.Property<object>(o, sortProperty)).ThenByDescending(o => o.SODate);
        else
            query = query.OrderBy(o => EF.Property<object>(o, sortProperty)).ThenBy(o => o.SODate);

        // 🎯 NEW DEFAULT: If sorting by ID (random Guid) or fallback, always prioritize latest records
        if (string.IsNullOrEmpty(sortBy) || sortBy.ToLower() == "id")
        {
            query = query.OrderByDescending(x => x.CreatedOn).ThenByDescending(x => x.SODate);
        }

        // 5. Pagination and Lightweight Data Fetch
        var orders = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new SaleOrderListDto
            {
                Id = o.Id,
                SoNumber = o.SONumber,
                SoDate = o.SODate,
                CustomerId = o.CustomerId,
                Status = o.Status,
                GatePassNo = o.GatePassNo,
                GrandTotal = o.GrandTotal,
                SubTotal = o.SubTotal,
                TotalTax = o.TotalTax,
                TaxType = o.TaxType,
                TdsPercent = o.TdsPercent,
                TdsAmount = o.TdsAmount,
                TcsPercent = o.TcsPercent,
                TcsAmount = o.TcsAmount,
                IgstAmount = o.IgstAmount,
                CgstAmount = o.CgstAmount,
                SgstAmount = o.SgstAmount,
                GuestName = o.GuestName,
                GuestPhone = o.GuestPhone,
                TotalQty = o.Items.Sum(i => i.Qty),
                CreatedBy = o.CreatedBy,
                Remarks = o.Remarks,
                CancelReason = o.CancelReason,
                CustomerName = "Loading...",
                Items = o.Items.Select(i => new SaleOrderItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Qty = i.Qty,
                    Unit = i.Unit,
                    Rate = i.Rate,
                    MRP = i.MRP,
                    DiscountAmount = i.DiscountAmount,
                    DiscountPercent = i.DiscountPercent,
                    GstPercent = i.GSTPercent,
                    TaxAmount = i.TaxAmount,
                    Total = i.Total,
                    WarehouseId = i.WarehouseId,
                    WarehouseName = i.WarehouseId != null ? _context.Warehouses.Where(w => w.Id == i.WarehouseId).Select(w => w.Name).FirstOrDefault() : null,
                    RackId = i.RackId,
                    RackName = i.RackId != null ? _context.Racks.Where(r => r.Id == i.RackId).Select(r => r.Name).FirstOrDefault() : null,
                    ManufacturingDate = i.MfgDate,
                    ExpiryDate = i.ExpDate
                }).ToList()
            })
            .ToListAsync();

        if (orders == null || !orders.Any())
            return (new List<SaleOrderListDto>(), 0, 0, 0, 0, 0, 0);

        // 6. External Service Mapping (Batched) & Return Status Check
        var orderIds = orders.Select(o => o.Id).ToList();
        var returnedQuantities = await _context.SaleReturnItems
            .Where(ri => orderIds.Contains(ri.SaleReturnHeader.SaleOrderId) && 
                         (ri.SaleReturnHeader.Status == "Confirmed" || ri.SaleReturnHeader.Status == "INWARDED"))
            .GroupBy(ri => ri.SaleReturnHeader.SaleOrderId)
            .Select(g => new { SaleOrderId = g.Key, TotalReturned = g.Sum(ri => ri.ReturnQty) })
            .ToDictionaryAsync(x => x.SaleOrderId, x => x.TotalReturned);

        var customerIds = orders
            .Select(o => o.CustomerId)
            .Where(id => id.HasValue && id.Value != Guid.Empty)
            .Cast<Guid>()
            .Distinct()
            .ToList();
        
        var customerDictionary = await _customerClient.GetCustomerNamesAsync(customerIds);

        foreach (var order in orders)
        {
            // Customer Name
            if (order.CustomerId.HasValue && customerDictionary != null && customerDictionary.TryGetValue(order.CustomerId.Value, out var name))
                order.CustomerName = name;
            else if (!string.IsNullOrEmpty(order.GuestName))
                order.CustomerName = order.GuestName;
            else if (!order.CustomerId.HasValue || order.CustomerId.Value == Guid.Empty)
                order.CustomerName = "Cash Customer";
            else
                order.CustomerName = "Unknown Customer";

            // Returnable Check: Disable if everything is already returned
            var returnedQty = returnedQuantities.ContainsKey(order.Id) ? returnedQuantities[order.Id] : 0;
            order.IsReturnable = returnedQty < order.TotalQty;
        }

        return (orders, totalCount, totalSalesAmount, pendingDispatchCount, unpaidOrdersCount, todayCount, monthCount);
    }

    public async Task<bool> UpdateSaleOrderStatusAsync(Guid id, string status)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Pehle Order fetch karein
                var order = await _context.SaleOrders.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId));
                if (order == null) return false;

                // 2. Agar status 'Confirmed' ho raha hai aur pehle se nahi tha
                if (status == "Confirmed" && order.Status != "Confirmed")
                {
                    // Order ke saare items nikaalein
                    var items = await _context.SaleOrderItems
                                              .Where(x => x.SaleOrderId == id)
                                              .ToListAsync();

                    foreach (var item in items)
                    {
                        // 🚀 UPDATE WAREHOUSE SPECIFIC STOCK (MINUS)
                        if (item.WarehouseId.HasValue && item.WarehouseId != Guid.Empty)
                        {
                            var whStock = await _context.WarehouseStocks
                                .FirstOrDefaultAsync(ws => ws.ProductId == item.ProductId && ws.WarehouseId == item.WarehouseId);

                            if (whStock != null)
                            {
                                whStock.Quantity -= item.Qty;
                            }
                            else
                            {
                                await _context.WarehouseStocks.AddAsync(new WarehouseStock
                                {
                                    ProductId = item.ProductId,
                                    WarehouseId = item.WarehouseId.Value,
                                    Quantity = -item.Qty,
                                    MinStock = 0,
                                    CompanyId = order.CompanyId,
                                    BranchId = order.BranchId
                                });
                            }
                        }

                        // 🆕 Record Inventory Transaction for Audit Trail
                        bool isQuick = order.SONumber.Contains("-Q-");
                        var saleTx = new InventoryTransaction(
                            item.ProductId,
                            -item.Qty, // Negative because it is REDUCING stock
                            isQuick ? "QuickSale" : "Sale",
                            order.SONumber,
                            item.WarehouseId,
                            item.RackId,
                            item.MfgDate,
                            item.ExpDate,
                            order.CompanyId,
                            order.BranchId,
                            item.ReferenceNumber, // Link back to source PO
                            item.BatchNumber     // Specific Batch
                        );
                        await _context.InventoryTransactions.AddAsync(saleTx);
                    }
                }
                // 3. Agar pehle 'Confirmed' tha aur ab status change ho raha hai (Reverse Stock)
                else if (order.Status == "Confirmed" && status != "Confirmed")
                {
                    var items = await _context.SaleOrderItems
                                              .Where(x => x.SaleOrderId == id)
                                              .ToListAsync();

                    foreach (var item in items)
                    {
                        // 🚀 RESTORE PHYSICAL WAREHOUSE STOCK (PLUS)
                        if (item.WarehouseId.HasValue && item.WarehouseId != Guid.Empty)
                        {
                            var whStock = await _context.WarehouseStocks
                                .FirstOrDefaultAsync(ws => ws.ProductId == item.ProductId && ws.WarehouseId == item.WarehouseId);

                            if (whStock != null)
                            {
                                whStock.Quantity += item.Qty;
                            }
                        }

                        // 🆕 Record Inventory Transaction (REVERSAL)
                        bool isQuick = order.SONumber.Contains("-Q-");
                        var reversalTx = new InventoryTransaction(
                            item.ProductId,
                            item.Qty, // Positive because it is READDING stock
                            (isQuick ? "QuickSale" : "Sale") + "-REVERSED",
                            order.SONumber,
                            item.WarehouseId,
                            item.RackId,
                            item.MfgDate,
                            item.ExpDate,
                            order.CompanyId,
                            order.BranchId,
                            item.ReferenceNumber,
                            item.BatchNumber
                        );
                        await _context.InventoryTransactions.AddAsync(reversalTx);
                    }
                }

                // 3. Status update karein aur save karein
                order.Status = status;
                var saved = await _context.SaveChangesAsync() > 0;
                
                await transaction.CommitAsync();

                if (saved && status == "Confirmed")
                {
                    if (order.CustomerId.HasValue && order.CustomerId.Value != Guid.Empty)
                    {
                        try
                        {
                            await _customerClient.RecordSaleAsync(
                                order.CustomerId.Value,
                                order.GrandTotal,
                                order.SONumber,
                                $"Sale Invoice generated: {order.SONumber}",
                                "System",
                                Guid.TryParse(order.BranchId, out var parsedBranchId) ? parsedBranchId : (Guid?)null,
                                order.CompanyId
                            );
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Customer Ledger sync error: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[UpdateSaleOrderStatus] Skipping ledger sync for Walking Customer: {order.GuestName}");
                    }
                }

                return saved;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error updating sale order status: {ex.Message}");
                throw; // Re-throw to handle in API layer
            }
        });
    }

    public async Task<SaleOrderDetailDto?> GetSaleOrderByIdAsync(Guid id)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        // 1. Database se Order aur uske Items fetch karein
        var order = await _context.SaleOrders
            .IgnoreQueryFilters()
            .Include(o => o.Items)
            .Where(o => o.Id == id && o.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || o.BranchId == branchId))
            .Select(o => new SaleOrderDetailDto
            {
                Id = o.Id,
                SoNumber = o.SONumber,
                SoDate = o.SODate,
                CustomerId = o.CustomerId,
                Status = o.Status,
                SubTotal = o.SubTotal,
                TotalTax = o.TotalTax,
                GrandTotal = o.GrandTotal,
                TaxType = o.TaxType,
                TdsPercent = o.TdsPercent,
                TdsAmount = o.TdsAmount,
                TcsPercent = o.TcsPercent,
                TcsAmount = o.TcsAmount,
                IgstAmount = o.IgstAmount,
                CgstAmount = o.CgstAmount,
                SgstAmount = o.SgstAmount,
                GuestName = o.GuestName,
                GuestPhone = o.GuestPhone,
                Remarks = o.Remarks,
                ExpectedDeliveryDate = o.ExpectedDeliveryDate,
                BranchId = o.BranchId,
                CompanyId = o.CompanyId,
                IsQuick = o.IsQuick,
                CancelReason = o.CancelReason,
                // Items ki mapping yahan karein
                Items = o.Items.Select(oi => new SaleOrderItemDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductName,
                    Qty = oi.Qty,
                    Unit = oi.Unit,
                    Rate = oi.Rate,
                    MRP = oi.MRP,
                    DiscountAmount = oi.DiscountAmount,
                    DiscountPercent = oi.DiscountPercent,
                    GstPercent = oi.GSTPercent,
                    TaxAmount = oi.TaxAmount,
                    Total = oi.Total,
                    WarehouseId = oi.WarehouseId,
                    WarehouseName = oi.WarehouseId != null ? _context.Warehouses.Where(w => w.Id == oi.WarehouseId).Select(w => w.Name).FirstOrDefault() : null,
                    RackId = oi.RackId,
                    RackName = oi.RackId != null ? _context.Racks.Where(r => r.Id == oi.RackId).Select(r => r.Name).FirstOrDefault() : null,
                    ManufacturingDate = oi.MfgDate,
                    ExpiryDate = oi.ExpDate
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (order == null) return null;

        // 2. Customer Name fetch karein Microservice se
        try
        {
            if (order.CustomerId.HasValue && order.CustomerId.Value != Guid.Empty)
            {
                var customer = await _customerClient.GetCustomerByIdAsync(order.CustomerId.Value);
                order.CustomerName = customer?.CustomerName ?? "Unknown Customer";
            }
            else
            {
                order.CustomerName = !string.IsNullOrEmpty(order.GuestName) ? order.GuestName : "Cash Customer";
            }
        }
        catch
        {
            order.CustomerName = "Name Fetch Failed";
        }

        return order;
    }

    public async Task<List<SaleOrderLookupDto>> GetOrdersByCustomerAsync(Guid customerId)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        return await _context.SaleOrders
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId && x.Status == "Confirmed" && x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId)) // Sirf confirmed orders [cite: 2026-02-05]
            .Select(x => new SaleOrderLookupDto
            {
                SaleOrderId = x.Id,
                SoNumber = x.SONumber, // Display ke liye number
                GrandTotal = x.GrandTotal
            }).ToListAsync();
    }

    public async Task<List<SaleOrderLookupDto>> GetCancelledOrdersByCustomerAsync(Guid customerId)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        
        var cancelledOrders = await _context.SaleOrders
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId && (x.Status == "Canceled" || x.Status == "Cancelled" || x.Status == "Void") && x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
            .Select(x => new SaleOrderLookupDto
            {
                SaleOrderId = x.Id,
                SoNumber = x.SONumber,
                GrandTotal = x.GrandTotal
            }).ToListAsync();

        var cancelledInvoices = await _context.SalesInvoices
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId && (x.Status == "Canceled" || x.Status == "Cancelled" || x.Status == "Void") && x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
            .Select(x => new SaleOrderLookupDto
            {
                SaleOrderId = x.Id,
                SoNumber = x.DeliveryChallanId != null
                    ? x.InvoiceNo + " (Challan: " + _context.DeliveryChallans
                        .Where(dc => dc.Id == x.DeliveryChallanId)
                        .Select(dc => dc.ChallanNo)
                        .FirstOrDefault() + ")"
                    : x.InvoiceNo,
                GrandTotal = x.GrandTotal
            }).ToListAsync();

        return cancelledOrders.Concat(cancelledInvoices).ToList();
    }

    public async Task<List<SaleOrderItemGridDto>> GetItemsForGridByOrderIdAsync(Guid saleOrderId)
    {
        var now = DateTime.Now;
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;

        // 1. Fetch Company Profile for Return Policy [cite: 2026-04-08]
        var company = await _companyClient.GetCompanyProfileAsync();
        int windowValue = company?.SaleReturnWindowValue ?? 72;
        string windowUnit = company?.SaleReturnWindowUnit ?? "Hours";

        // 2. Calculate dynamic limit date
        double totalHours = windowUnit switch
        {
            "Hours" => windowValue,
            "Days" => windowValue * 24,
            "Months" => windowValue * 30 * 24,
            _ => windowValue
        };

        var limitDate = now.AddHours(-totalHours);

        var items = await _context.SaleOrderItems
            .Include(x => x.SaleOrder)
            .Include(x => x.Warehouse)
            .Include(x => x.Rack)
            .AsNoTracking()
            .Where(x => x.SaleOrderId == saleOrderId && x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
            .ToListAsync();

        var result = new List<SaleOrderItemGridDto>();

        foreach (var x in items)
        {
            var dto = new SaleOrderItemGridDto
            {
                ProductId = x.ProductId,
                ProductName = x.ProductName,
                Rate = x.Rate,
                DiscountPercent = x.DiscountPercent,
                DiscountAmount = x.DiscountAmount,
                TaxPercentage = x.GSTPercent,
                MfgDate = x.MfgDate,
                ExpDate = x.ExpDate,
                WarehouseId = x.WarehouseId,
                WarehouseName = x.Warehouse?.Name,
                RackId = x.RackId,
                RackName = x.Rack?.Name,

                // Dynamic Policy Calculation
                IsReturnable = x.SaleOrder.SODate >= limitDate,
                ReturnWindowRemainingHours = Math.Max(0, totalHours - (now - x.SaleOrder.SODate).TotalHours)
            };

            // Use WarehouseStocks instead of summing thousands of transactions
            dto.CurrentStock = await _context.WarehouseStocks
                .AsNoTracking()
                .Where(tx => tx.ProductId == x.ProductId && tx.CompanyId == companyId && tx.WarehouseId == x.WarehouseId)
                .SumAsync(tx => (decimal?)tx.Quantity) ?? 0;

            // CRITICAL FIX: Original Sold (10) minus Already Returned (5) = Display (5)
            var returnedQty = await _context.SaleReturnItems
                .Where(sr => sr.ProductId == x.ProductId &&
                             sr.SaleReturnHeader.SaleOrderId == saleOrderId &&
                             sr.CompanyId == companyId &&
                             (sr.SaleReturnHeader.Status == "Confirmed" || sr.SaleReturnHeader.Status == "INWARDED") &&
                             (x.MfgDate == null || sr.MfgDate == x.MfgDate) &&
                             (x.ExpDate == null || sr.ExpDate == x.ExpDate))
                .SumAsync(sr => (decimal?)sr.ReturnQty) ?? 0;
            
            dto.SoldQty = x.Qty - returnedQty;

            // Fetch GRN and PO references based on Product/Warehouse/Rack/Batch metadata
            var grn = await _context.GRNDetails
                .Include(g => g.GRNHeader)
                .ThenInclude(h => h.PurchaseOrder)
                .Where(g => g.ProductId == x.ProductId &&
                            g.WarehouseId == x.WarehouseId &&
                            g.RackId == x.RackId &&
                            g.CompanyId == companyId &&
                            (!x.MfgDate.HasValue || g.MfgDate.Value.Date == x.MfgDate.Value.Date) &&
                            (!x.ExpDate.HasValue || g.ExpDate.Value.Date == x.ExpDate.Value.Date))
                .OrderByDescending(g => g.Id)
                .FirstOrDefaultAsync();

            dto.GrnNumber = grn?.GRNHeader?.GRNNumber;
            dto.RefNo = grn?.GRNHeader?.PurchaseOrder?.PoNumber;

            if (dto.SoldQty > 0)
            {
                result.Add(dto);
            }
        }

        return result;
    }


    public async Task<List<PendingSODto>> GetPendingSaleOrdersAsync()
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        // 1. Fetch SOs that are Confirmed
        // 2. SAFETY LOCK: Exclude SOs that already have an "At-Gate" Gate Pass (Status 1)
        var orders = await _context.SaleOrders
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.Status == "Confirmed" && (x.GatePassNo == null || x.GatePassNo == "") && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
            .OrderByDescending(x => x.SODate)
            .Select(x => new PendingSODto
            {
                Id = x.Id,
                SoNumber = x.SONumber,
                SoDate = x.SODate,
                Status = x.Status,
                CustomerId = x.CustomerId,
                TotalQty = x.Items.Sum(i => i.Qty)
            })
            .ToListAsync();

        if (orders == null || !orders.Any()) return new List<PendingSODto>();

        var customerIds = orders
            .Select(o => o.CustomerId)
            .Where(id => id.HasValue && id.Value != Guid.Empty)
            .Cast<Guid>()
            .Distinct()
            .ToList();
            
        var customerDictionary = await _customerClient.GetCustomerNamesAsync(customerIds);

        foreach (var order in orders)
        {
            if (order.CustomerId.HasValue && customerDictionary != null && customerDictionary.TryGetValue(order.CustomerId.Value, out var name))
                order.CustomerName = name;
            else if (!order.CustomerId.HasValue || order.CustomerId.Value == Guid.Empty)
                order.CustomerName = "Cash Customer";
            else
                order.CustomerName = "Unknown Customer";
        }

        return orders;
    }

    public async Task<bool> ExistsByPhoneAsync(string phone, Guid companyId)
    {
        var existsInSo = await _context.SaleOrders
            .AnyAsync(x => x.GuestPhone == phone && x.CompanyId == companyId);

        if (existsInSo) return true;

        return await _context.SalesInvoices
            .AnyAsync(x => x.GuestPhone == phone && x.CompanyId == companyId);
    }
}

