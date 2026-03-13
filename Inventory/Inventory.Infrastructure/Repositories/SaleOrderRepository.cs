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

public class SaleOrderRepository : ISaleOrderRepository
{
    private readonly InventoryDbContext _context;
    private IDbContextTransaction? _transaction; 
    private readonly HttpClient _httpClient;
    private readonly ICustomerClient _customerClient;
   
    public SaleOrderRepository(InventoryDbContext context, IHttpClientFactory httpClientFactory, ICustomerClient customerClient)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _customerClient = customerClient;
       
        if (httpClientFactory == null) throw new ArgumentNullException(nameof(httpClientFactory));

        _httpClient = httpClientFactory.CreateClient("CustomerService");
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

    // Guid aur Decimal support ke saath methods
    public async Task<decimal> GetAvailableStockAsync(Guid productId)
    {
        return await _context.Products
            .Where(p => p.Id == productId)
            .Select(p => p.CurrentStock)
            .FirstOrDefaultAsync();
    }

    public async Task UpdateProductStockAsync(Guid productId, decimal adjustmentQty)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product != null)
        {
            product.CurrentStock += adjustmentQty;
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<string> GetLastSONumberAsync() =>
        await _context.SaleOrders.OrderByDescending(x => x.Id).Select(x => x.SONumber).FirstOrDefaultAsync();

    public async Task<int> SaveAsync(SaleOrder order)
    {
        _context.SaleOrders.Add(order);
        await _context.SaveChangesAsync();
        return order.Id;
    }
    public async Task<List<StockExportDto>> GetSaleReportDataAsync(List<int> orderIds) // logic according to integer IDs
    {
        // Selected Orders ke Product IDs fetch karein
        return await _context.SaleOrderItems
            .Where(si => orderIds.Contains(si.SaleOrderId)) // Filter by selected integer IDs
            .GroupBy(si => new { si.ProductId, si.ProductName, si.Unit })
            .Select(group => new StockExportDto
            {
                ProductName = group.Key.ProductName,
                Unit = group.Key.Unit,
                TotalReceived = _context.GRNDetails.Where(g => g.ProductId == group.Key.ProductId).Sum(x => x.ReceivedQty),
                TotalRejected = _context.GRNDetails.Where(g => g.ProductId == group.Key.ProductId).Sum(x => x.RejectedQty),
                AvailableStock = (_context.GRNDetails.Where(g => g.ProductId == group.Key.ProductId).Sum(x => x.ReceivedQty) -
                                  _context.GRNDetails.Where(g => g.ProductId == group.Key.ProductId).Sum(x => x.RejectedQty)) -
                                 (_context.SaleOrderItems.Where(si => si.ProductId == group.Key.ProductId && si.SaleOrder.Status == "Confirmed").Sum(si => (decimal?)si.Qty) ?? 0)
            }).ToListAsync();
    }

    public async Task<(List<SaleOrderListDto> Data, int TotalCount, decimal TotalSalesAmount, int PendingDispatchCount, int UnpaidOrdersCount)> GetAllSaleOrdersAsync(
     string searchTerm,
     int pageNumber,
     int pageSize,
     string sortBy,
     string sortOrder)
    {
        // 1. Optimized Base Query
        var query = _context.SaleOrders
            .AsNoTracking()
            .AsQueryable();

        // 2. Searching logic [cite: 2026-02-03]
        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.Trim().ToLower();

            // Fetch matching customer IDs from External Service
            var matchingCustomerIds = new List<int>();
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
                matchingCustomerIds.Contains(o.CustomerId));
        }

        // 🎯 3. Calculate Global Stats (Before Pagination)
        var totalCount = await query.CountAsync();
        var totalSalesAmount = await query.Where(o => o.Status == "Confirmed").SumAsync(o => (decimal?)o.GrandTotal) ?? 0;
        var pendingDispatchCount = await query.Where(o => o.Status == "Confirmed" && (o.GatePassNo == null || o.GatePassNo == "")).CountAsync();
        
        // Note: Unpaid count is hard to calculate globally without Finance Service data 
        var unpaidOrdersCount = 0; 

        // 4. Enhanced Sorting Logic
        bool isDesc = sortOrder?.ToLower() == "desc" || string.IsNullOrEmpty(sortOrder);
        string sortProperty = (sortBy ?? "").ToLower() switch
        {
            "sonumber" => "SONumber",
            "sodate" => "SODate",
            "status" => "Status",
            "grandtotal" => "GrandTotal",
            "createdat" => "CreatedAt",
            _ => "CreatedAt" // Default order by CreatedAt desc
        };

        if (isDesc)
            query = query.OrderByDescending(o => EF.Property<object>(o, sortProperty));
        else
            query = query.OrderBy(o => EF.Property<object>(o, sortProperty));

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
                TotalQty = o.Items.Sum(i => i.Qty),
                CustomerName = "Loading..."
            })
            .ToListAsync();

        if (orders == null || !orders.Any())
            return (new List<SaleOrderListDto>(), 0, 0, 0, 0);

        // 6. External Service Mapping (Batched)
        var customerIds = orders.Select(o => o.CustomerId).Distinct().ToList();
        var customerDictionary = await GetCustomerNamesFromService(customerIds);

        foreach (var order in orders)
        {
            if (customerDictionary != null && customerDictionary.TryGetValue(order.CustomerId, out var name))
                order.CustomerName = name;
            else
                order.CustomerName = "Unknown Customer";
        }

        return (orders, totalCount, totalSalesAmount, pendingDispatchCount, unpaidOrdersCount);
    }

    public async Task<bool> UpdateSaleOrderStatusAsync(int id, string status)
    {
        // 1. Pehle Order fetch karein
        var order = await _context.SaleOrders.FindAsync(id);
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
                // Product table se stock kam karein
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    // Current stock mein se order qty minus kar dein
                    product.CurrentStock -= item.Qty;
                }
            }
        }

        // 3. Status update karein aur save karein
        order.Status = status;
        var saved = await _context.SaveChangesAsync() > 0;

        if (saved && status == "Confirmed")
        {
            try
            {
                await _customerClient.RecordSaleAsync(
                    order.CustomerId,
                    order.GrandTotal,
                    order.SONumber,
                    $"Sale Invoice generated: {order.SONumber}",
                    "System"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Customer Ledger sync error: {ex.Message}");
            }
        }

        return saved;
    }

    public async Task<SaleOrderDetailDto?> GetSaleOrderByIdAsync(int id)
    {
        // 1. Database se Order aur uske Items fetch karein
        var order = await _context.SaleOrders
      
            .Include(o => o.Items)
            .Where(o => o.Id == id)
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
                // Items ki mapping yahan karein
                Items = o.Items.Select(oi => new SaleOrderItemDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductName,
                    Qty = oi.Qty,
                    Unit = oi.Unit,
                    Rate = oi.Rate,
                    DiscountPercent = oi.DiscountPercent,
                    GstPercent = oi.GSTPercent,
                    TaxAmount = oi.TaxAmount,
                    Total = oi.Total,
                    RackName = _context.Products.Where(p => p.Id == oi.ProductId).Select(p => p.DefaultRack != null ? p.DefaultRack.Name : "N/A").FirstOrDefault()
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (order == null) return null;

        // 2. Customer Name fetch karein Microservice se
        try
        {
            // Single customer name fetch call
            var response = await _httpClient.GetAsync($"api/customers/{order.CustomerId}/name");
            if (response.IsSuccessStatusCode)
            {
                order.CustomerName = await response.Content.ReadAsStringAsync();
            }
        }
        catch
        {
            order.CustomerName = "Name Fetch Failed";
        }

        return order;
    }

    public async Task<List<SaleOrderLookupDto>> GetOrdersByCustomerAsync(int customerId)
    {
        return await _context.SaleOrders
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId && x.Status == "Confirmed") // Sirf confirmed orders [cite: 2026-02-05]
            .Select(x => new SaleOrderLookupDto
            {
                SaleOrderId = x.Id,
                SoNumber = x.SONumber // Display ke liye number
            }).ToListAsync();
    }



    public async Task<List<SaleOrderItemGridDto>> GetItemsForGridByOrderIdAsync(int saleOrderId)
    {
        return await _context.SaleOrderItems
            .AsNoTracking()
            .Where(x => x.SaleOrderId == saleOrderId)
            .Select(x => new SaleOrderItemGridDto
            {
                ProductId = x.ProductId,
                ProductName = x.ProductName,
                Rate = x.Rate,
                DiscountPercent = x.DiscountPercent,
                TaxPercentage = x.GSTPercent,
                MfgDate = x.MfgDate,
                ExpDate = x.ExpDate,

                // CRITICAL FIX: Original Sold (10) minus Already Returned (5) = Display (5)
                // Isse user ko wahi dikhega jo uske paas bacha hai
                SoldQty = x.Qty - (_context.SaleReturnItems
                    .Where(sr => sr.ProductId == x.ProductId &&
                                sr.SaleReturnHeader.SaleOrderId == saleOrderId &&
                                sr.SaleReturnHeader.Status == "Confirmed")
                    .Sum(sr => (decimal?)sr.ReturnQty) ?? 0)
            })
            .Where(x => x.SoldQty > 0) // Agar pura maal wapas aa gaya (0 bacha), toh grid mein mat dikhao
            .ToListAsync();
    }


    public async Task<List<PendingSODto>> GetPendingSaleOrdersAsync()
    {
        // 1. Fetch SOs that are Confirmed
        // 2. SAFETY LOCK: Exclude SOs that already have an "At-Gate" Gate Pass (Status 1)
        var orders = await _context.SaleOrders
            .AsNoTracking()
            .Where(x => x.Status == "Confirmed" && (x.GatePassNo == null || x.GatePassNo == ""))
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

        var customerIds = orders.Select(o => o.CustomerId).Distinct().ToList();
        var customerDictionary = await GetCustomerNamesFromService(customerIds);

        foreach (var order in orders)
        {
            if (customerDictionary != null && customerDictionary.TryGetValue(order.CustomerId, out var name))
                order.CustomerName = name;
            else
                order.CustomerName = "Unknown Customer";
        }

        return orders;
    }

    // Helper method jo actual Microservice call handle karega
    private async Task<Dictionary<int, string>> GetCustomerNamesFromService(List<int> customerIds)
    {
        try
        {
            // Note: URL wahi hona chahiye jo Customers.API ke controller mein defined hai
            var response = await _httpClient.PostAsJsonAsync("api/customers/get-names", customerIds);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<Dictionary<int, string>>();
                return data ?? new Dictionary<int, string>();
            }
        }
        catch (Exception ex)
        {
            // Agar Microservice band hai toh crash na ho
            Console.WriteLine($"Microservice call failed: {ex.Message}");
        }

        return new Dictionary<int, string>();
    }
}