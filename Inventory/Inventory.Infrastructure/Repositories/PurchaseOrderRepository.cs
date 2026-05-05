using DinkToPdf;
using DinkToPdf.Contracts;
using Inventory.Application.Clients;
using Inventory.Application.Clients.DTOs;
using Inventory.Application.Common.DTOs;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.PurchaseOrders.DTOs;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Inventory.Application.Services;
using System.Linq;

public sealed class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly ICurrentUserService _currentUserService;
    private readonly InventoryDbContext _context;
    private readonly IConverter _converter;
    private readonly INotificationRepository _notificationRepository;
    private readonly ICompanyClient _companyClient;
    private readonly IServiceScopeFactory _scopeFactory;

    public PurchaseOrderRepository(InventoryDbContext context, 
        INotificationRepository notificationRepository,
        IConverter converter,
        ICompanyClient companyClient,
        IServiceScopeFactory scopeFactory,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _converter = converter;
        _notificationRepository = notificationRepository;
        _companyClient = companyClient;
        _scopeFactory = scopeFactory;
        _currentUserService = currentUserService;
    }

    public async Task AddAsync(PurchaseOrder po, CancellationToken ct)
    {
        await _context.PurchaseOrders.AddAsync(po, ct);
    }    

    public async Task<string?> GetLastPoNumberAsync()
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        return await _context.PurchaseOrders.AsNoTracking()
            .Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
            .OrderByDescending(x => x.CreatedOn)
            .Select(x => x.PoNumber)
            .FirstOrDefaultAsync();
    }

    public async Task<string?> GetLatestPoNumberAsync()
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        return await _context.PurchaseOrders.AsNoTracking()
            .Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
            .OrderByDescending(x => x.CreatedOn)
            .Select(x => x.PoNumber)
            .FirstOrDefaultAsync();
    }

    public async Task<(IEnumerable<PurchaseOrder> Items, int TotalCount)> GetPagedOrdersAsync(
    int pageIndex, int pageSize, string? sortField, string? sortOrder, string? filter)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        // 1. Base Query with Eager Loading for Items and Products
        var query = _context.PurchaseOrders.AsNoTracking()
            .Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
            .Include(x => x.Items)
                .ThenInclude(i => i.Product) // Product data load karega taaki null error na aaye
            .AsSplitQuery()
            .AsNoTracking()
            .AsQueryable();

        // 2. Filtering Logic (Multi-column search)
        if (!string.IsNullOrWhiteSpace(filter))
        {
            var cleanFilter = filter.Trim();

            query = query.Where(x =>
                EF.Functions.Like(x.PoNumber, $"%{cleanFilter}%") ||
                EF.Functions.Like(x.Id.ToString(), $"%{cleanFilter}%") ||
                EF.Functions.Like(x.Status, $"%{cleanFilter}%") ||
                EF.Functions.Like(x.SupplierName, $"%{cleanFilter}%") // Direct DB column search
            );
        }

        // 3. Sorting Logic
        if (!string.IsNullOrEmpty(sortField))
        {
            string mappedField = sortField.ToLower() switch
            {
                "ponumber" => "PoNumber",
                "podate" => "PoDate",
                "grandtotal" => "GrandTotal",
                "status" => "Status",
                "id" => "Id",
                "suppliername" => "SupplierName",
                _ => "PoDate"
            };

            query = (sortOrder?.ToLower() == "desc")
                ? query.OrderByDescending(x => EF.Property<object>(x, mappedField))
                : query.OrderBy(x => EF.Property<object>(x, mappedField));
        }
        else
        {
            query = query.OrderByDescending(x => x.PoDate).ThenByDescending(x => x.CreatedOn);
        }

        // 4. Execution
        var totalCount = await query.CountAsync();

        var items = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <summary>
    /// bind main grid polist
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<(IEnumerable<PurchaseOrder> Data, int Total, decimal TotalAmount, int TodayCount, int MonthCount)> GetDateRangePagedOrdersAsync(GetPurchaseOrdersRequest request)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = !string.IsNullOrEmpty(request.BranchId) ? request.BranchId : _currentUserService.BranchId;

        // STEP 1: Base Query - AsNoTracking use karein fast read ke liye
        var query = _context.PurchaseOrders
            .AsNoTracking()
            .Where(x => x.IsQuick == request.IsQuick && x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
            .AsQueryable();

        // 1. GLOBAL SEARCH FIX (Including 'Received' status logic)
        if (!string.IsNullOrWhiteSpace(request.Filter))
        {
            var searchTerm = request.Filter.Trim().ToLower();
            query = query.Where(x =>
                (x.PoNumber != null && x.PoNumber.ToLower().Contains(searchTerm)) ||
                (x.SupplierName != null && x.SupplierName.ToLower().Contains(searchTerm)) ||
                (x.Status != null && x.Status.ToLower().Contains(searchTerm)) ||
                ("received".Contains(searchTerm) && x.GrnHeaders.Any())
            );
        }

        // 2. DATE RANGE
        if (request.FromDate.HasValue) query = query.Where(x => x.PoDate >= request.FromDate.Value);
        if (request.ToDate.HasValue)
        {
            var endOfToDate = request.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.PoDate <= endOfToDate);
        }

        // 3. COLUMN SPECIFIC FILTERS FIX (Especially for Status 'Received')
        if (request.Filters != null && request.Filters.Any())
        {
            foreach (var f in request.Filters)
            {
                var val = (f.Value ?? "").Trim().ToLower();
                var field = (f.Field ?? "").Trim().ToLower();

                if (string.IsNullOrEmpty(val)) continue;

                query = field switch
                {
                    "status" => query.Where(x => 
                        (x.Status != null && x.Status.ToLower().Contains(val)) ||
                        ("received".Contains(val) && x.GrnHeaders.Any())
                    ),
                    "ponumber" or "po no." => query.Where(x => x.PoNumber != null && x.PoNumber.ToLower().Contains(val)),
                    "suppliername" => query.Where(x => x.SupplierName != null && x.SupplierName.ToLower().Contains(val)),
                    "id" => query.Where(x => x.Id.ToString().Contains(val)),
                    _ => query
                };
            }
        }

        // 🎯 STEP 2: Calculate Dashboard Stats (Before Pagination)
        var total = await query.CountAsync();
        
        // Total amount based on the current filtered view
        var totalAmount = await query.SumAsync(x => (decimal?)x.GrandTotal) ?? 0;

        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var tomorrow = today.AddDays(1);

        // Counts based on the current filtered view (ignoring date filters for global counts is common but here we stick to current view base)
        var todayCount = await query.Where(x => x.PoDate >= today && x.PoDate < tomorrow).CountAsync();
        var monthCount = await query.Where(x => x.PoDate >= monthStart).CountAsync();

        // 4. DYNAMIC SORTING FIX (Considering 'Received' status and all columns)
        bool isDesc = request.SortOrder?.ToLower() == "desc";
        string sortField = request.SortField?.ToLower().Trim();

        query = sortField switch
        {
            "status" => isDesc 
                ? query.OrderByDescending(x => x.GrnHeaders.Any() ? "Received" : x.Status)
                : query.OrderBy(x => x.GrnHeaders.Any() ? "Received" : x.Status),
            "ponumber" => isDesc ? query.OrderByDescending(x => x.PoNumber) : query.OrderBy(x => x.PoNumber),
            "suppliername" => isDesc ? query.OrderByDescending(x => x.SupplierName) : query.OrderBy(x => x.SupplierName),
            "grandtotal" or "amount" or "totalamount" => isDesc ? query.OrderByDescending(x => x.GrandTotal) : query.OrderBy(x => x.GrandTotal),
            "podate" or "date" => isDesc ? query.OrderByDescending(x => x.PoDate).ThenByDescending(x => x.CreatedOn) : query.OrderBy(x => x.PoDate).ThenByDescending(x => x.CreatedOn),
            "expecteddeliverydate" => isDesc ? query.OrderByDescending(x => x.ExpectedDeliveryDate) : query.OrderBy(x => x.ExpectedDeliveryDate),
            "createdby" => isDesc ? query.OrderByDescending(x => x.CreatedBy) : query.OrderBy(x => x.CreatedBy),
            "createdon" or "createddate" => isDesc ? query.OrderByDescending(x => x.CreatedOn) : query.OrderBy(x => x.CreatedOn),
            "id" => isDesc ? query.OrderByDescending(x => x.Id).ThenByDescending(x => x.PoDate) : query.OrderBy(x => x.Id).ThenBy(x => x.PoDate),
            _ => query.OrderByDescending(x => x.PoDate).ThenByDescending(x => x.CreatedOn)
        };

        // 🎯 NEW DEFAULT: If sorting by ID (random Guid) or fallback, always prioritize latest records
        if (string.IsNullOrEmpty(sortField) || sortField == "id")
        {
            query = query.OrderByDescending(x => x.PoDate).ThenByDescending(x => x.CreatedOn);
        }

        // STEP 3: Optimized Data Fetch (Fetch only required items)
        var data = await query
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Include(x => x.Items)
                .ThenInclude(i => i.Product)
            .Include(x => x.GrnHeaders)
                .ThenInclude(h => h.GRNItems)
            .AsSplitQuery() 
            .ToListAsync();

        // 🎯 STEP 4: Post-process to calculate totals for UI logic
        var poGrnNumbers = data.SelectMany(x => x.GrnHeaders).Select(h => h.GRNNumber).Distinct().ToList();
        
        // Fetch all return items for these GRNs in one go
        var poGrnNumbersLower = poGrnNumbers.Select(n => n.Trim().ToLower()).ToList();
        var allReturnItems = await _context.PurchaseReturnItems
            .Where(ri => ri.CompanyId == companyId)
            .ToListAsync();
        
        // Filter in-memory for robust matching
        allReturnItems = allReturnItems.Where(ri => poGrnNumbersLower.Contains(ri.GrnRef.Trim().ToLower())).ToList();

        foreach (var po in data)
        {
            var poGrnsLower = po.GrnHeaders.Select(h => h.GRNNumber.Trim().ToLower()).ToList();
            var poReturnItems = allReturnItems.Where(ri => poGrnsLower.Contains(ri.GrnRef.Trim().ToLower())).ToList();

            po.TotalRejected = po.GrnHeaders.SelectMany(h => h.GRNItems ?? new List<GRNDetail>()).Sum(d => d.RejectedQty);
            po.TotalReturned = poReturnItems.Sum(ri => ri.ReturnQty);

            // 🎯 BUSINESS RULE: If there are unresolved rejections, PO cannot be "Received"
            // Override DB status dynamically to ensure UI always reflects true state
            var netRejected = po.TotalRejected - po.TotalReturned;
            if (netRejected > 0 && po.Status == "Received")
            {
                po.Status = "Partially Received";
            }

            // 🎯 STEP 5: Populate child items with Rejection/Return info
            foreach (var item in po.Items)
            {
                item.RejectedQty = po.GrnHeaders
                    .SelectMany(h => h.GRNItems ?? new List<GRNDetail>())
                    .Where(gd => gd.ProductId == item.ProductId)
                    .Sum(gd => gd.RejectedQty);

                item.ReturnedQty = poReturnItems
                    .Where(ri => ri.ProductId == item.ProductId)
                    .Sum(ri => ri.ReturnQty);
            }
        }

        return (data, total, totalAmount, todayCount, monthCount);
    }
    public async Task<PurchaseOrder?> GetByIdWithItemsAsync(Guid id, CancellationToken ct)
    {
      var companyId = _currentUserService.CompanyId ?? Guid.Empty;
      var branchId = _currentUserService.BranchId;
      var data= await _context.PurchaseOrders.AsNoTracking()
            .Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
            .Include(x => x.Items) // Yeh child table 'PurchaseOrderItems' se data layega
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return data;
    }

    public void Update(PurchaseOrder po) => _context.PurchaseOrders.Update(po);

    public void RemoveItem(PurchaseOrderItem item) => _context.PurchaseOrderItems.Remove(item);

    public async Task<bool> DeleteItemAsync(Guid itemId)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        var item = await _context.PurchaseOrderItems.FirstOrDefaultAsync(x => x.Id == itemId && x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId));
        if (item == null) return false;

        _context.PurchaseOrderItems.Remove(item);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task UpdatePOTotalsAsync(Guid poId)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        var items = await _context.PurchaseOrderItems.AsNoTracking()
                                  .Where(x => x.PurchaseOrderId == poId && x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
                                  .ToListAsync();

        var po = await _context.PurchaseOrders.FindAsync(poId);

        if (po != null)
        {
            // 0. TotalQuantity
            po.TotalQuantity = items.Sum(x => x.Qty);

            // 1. SubTotal hamesha (Total - TaxAmount) hona chahiye agar aapke DB mein Total inclusive hai
            // Ya phir agar aapke paas TaxableAmount ka alag column hai toh wo use karein.
            po.SubTotal = items.Sum(x => x.Total - x.TaxAmount);

            // 2. TotalTax bilkul sahi hai
            po.TotalTax = items.Sum(x => x.TaxAmount);

            // 3. GrandTotal ab accurate aayega
            po.GrandTotal = po.SubTotal + po.TotalTax;

            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> BulkDeleteItemsAsync(List<Guid> itemIds)
    {
        var items = await _context.PurchaseOrderItems.AsNoTracking()
                                  .Where(x => itemIds.Contains(x.Id))
                                  .ToListAsync();

        if (!items.Any()) return false;

        _context.PurchaseOrderItems.RemoveRange(items);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<PurchaseOrder> GetByIdAsync(Guid id)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        return await _context.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || p.BranchId == branchId));
    }

    public void Delete(PurchaseOrder po)
    {
        _context.PurchaseOrders.Remove(po);
    }

    public async Task<List<PurchaseOrder>> GetByIdsAsync(List<Guid> ids)
    {
        return await _context.PurchaseOrders
            .Include(x => x.Items)
            .Where(x => ids.Contains(x.Id))
            .ToListAsync();
    }

    /// <summary>
    /// /////////
    /// </summary>
    /// <param name="po"></param>
    /// <returns></returns>
    public Task UpdateAsync(PurchaseOrder po)
    {
        _context.PurchaseOrders.Attach(po);
        _context.Entry(po).Property(x => x.Status).IsModified = true;
        return Task.CompletedTask;
    }

    public async Task<bool> SaveChangesAsync()
    {
        // Agar 1 ya usse zyada rows affect hui hain toh true return karega
        return (await _context.SaveChangesAsync()) > 0;
    }
    public async Task<PurchaseOrder> GetByIdAsyncForUpdateStatus(Guid id)
    {
        // PO ke saath uske items ko bhi load karna behtar hota hai (Optional)
        return await _context.PurchaseOrders.AsNoTracking()
                             .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<bool> UpdatePOStatusAsync(Guid id, string status)
    {
        // Id check aur fetch
        var po = await _context.PurchaseOrders.FindAsync(id);
        if (po == null) return false;

        po.Status = status; // Status database mein update hua

        if (await _context.SaveChangesAsync() > 0)
        {
            // FIX: Sirf tabhi notification bhejein jab status 'Draft' na ho
            if (status != "Draft")
            {
                // 3 Specific Updates: Submitted, Rejected, aur Approved
                string title = status switch
                {
                    "Approved" => "PO Approved",
                    "Rejected" => "PO Rejected",
                    "Submitted" => "PO Submitted",
                    _ => "PO Status Updated" // Backup title
                };

                string message = $"Purchase Order {po.PoNumber} status has been changed to {status}.";

                // DATA SAVE HOGA APPNOTIFICATIONS TABLE MEIN
                await _notificationRepository.AddNotificationAsync(
                    title,
                    message,
                    "PO",
                    "/app/inventory/polist",
                    po.BranchId,
                    po.CompanyId
                );
            }

            return true;
        }

        return false;
    }

    public async Task<IEnumerable<PendingPODto>> GetPendingPurchaseOrdersAsync()
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        // 1. Fetch POs that are Approved and have pending quantities
        // 2. SAFETY LOCK: Exclude POs that already have an "At-Gate" Gate Pass (Status 1)
        return await _context.PurchaseOrders.AsNoTracking()
            .Where(po => po.CompanyId == companyId && (po.Status == "Approved" || po.Status == "Partially Received") 
                && po.Items.Any(i => i.Qty > i.ReceivedQty) && (string.IsNullOrEmpty(branchId) || po.BranchId == branchId))
            .Where(po => !_context.GatePasses.Any(gp => 
                gp.ReferenceType == 1 && // 1 = PurchaseOrder
                gp.ReferenceId == po.Id.ToString() && 
                gp.Status == 1)) // 1 = Entered/At-Gate
            .OrderByDescending(po => po.CreatedOn)
            .Select(po => new PendingPODto
            {
                Id = po.Id,
                PoNumber = po.PoNumber,
                SupplierName = po.SupplierName,
                PoDate = po.PoDate,
                Status = po.Status,
                ExpectedQty = po.Items.Sum(x => x.Qty - x.ReceivedQty) // Show balance quantity
            })
            .ToListAsync();
    }
    public async Task<IEnumerable<POItemForGRNDto>> GetPOItemsForGRNAsync(Guid poId)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        var poItems = await _context.PurchaseOrderItems.AsNoTracking()
            .Where(poi => poi.PurchaseOrderId == poId && poi.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || poi.BranchId == branchId))
            .Include(poi => poi.Product)
            .ToListAsync();

        var receivedQuantities = await _context.GRNDetails.AsNoTracking()
            .Where(gi => gi.GRNHeader.PurchaseOrderId == poId && gi.CompanyId == companyId)
            .GroupBy(gi => gi.ProductId)
            .Select(g => new { ProductId = g.Key, Total = g.Sum(x => (decimal?)x.ReceivedQty) ?? 0 })
            .ToListAsync();

       
        return poItems.Select(poi => new POItemForGRNDto
        {
            ProductId = poi.ProductId,
            ProductName = poi.Product?.Name,
            OrderedQty = poi.Qty,
            UnitPrice = poi.Rate,
            AlreadyReceivedQty = receivedQuantities.FirstOrDefault(rq => rq.ProductId == poi.ProductId)?.Total ?? 0,
            ManufacturingDate = poi.MfgDate,
            ExpiryDate = poi.ExpDate,
            IsExpiryRequired = poi.Product?.IsExpiryRequired ?? false
        }).ToList();
    }

    public async Task<POHeaderDetailsDto?> GetPOHeaderAsync(Guid lastPurchaseOrderId)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        return await _context.PurchaseOrders.AsNoTracking()
        .Where(x => x.Id == lastPurchaseOrderId && x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
        
        .Select(x => new POHeaderDetailsDto
        {
            PurchaseOrderId = x.Id,
            ExpectedDeliveryDate=x.ExpectedDeliveryDate,
            SupplierId = x.SupplierId,      // int
            SupplierName = x.SupplierName,
            PriceListId = x.PriceListId, 
            
            Remarks = x.Remarks,
            PoNumber = x.PoNumber,
            PoDate = DateTime.Now           // Hamesha current date rakhein
        }).FirstOrDefaultAsync();
    }

    public async Task<ProductPriceDto?> GetPriceListRateAsync( Guid productId, Guid priceListId)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        return await _context.PriceListItems.AsNoTracking()
            .Where(pi => pi.PriceListId == priceListId && pi.ProductId == productId && pi.CompanyId == companyId)
            .Select(pi => new ProductPriceDto
            {
                ProductId = pi.ProductId,
                Rate = pi.Rate, // From dbo.PriceListItems
                Unit = pi.Unit, // From dbo.PriceListItems
                                // From dbo.Products.DefaultGst
                GstPercent = pi.Product.DefaultGst ?? 0,
                DiscountPercent = pi.DiscountPercent // Fixed: Mapping discount from price list
            })
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// bulk submitted
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    public async Task<bool> BulkSentForApprovalAsync(List<Guid> ids)
    {
        // 1. Fetch Draft or Rejected POs
        var pos = await _context.PurchaseOrders
            .Where(x => ids.Contains(x.Id) && (x.Status == "Draft" || x.Status == "Rejected"))
            .ToListAsync();

        if (pos == null || !pos.Any())
        {
            return false;
        }

        // 2. Status Update
        foreach (var po in pos)
        {
            po.Status = "Submitted"; // Status Submitted kiya
            po.ModifiedOn = DateTime.Now;
        }

        // 3. Save Changes
        if (await _context.SaveChangesAsync() > 0)
        {
            // --- BULK NOTIFICATION TRIGGER ---
            // Har PO ke liye alag alag nahi, balki ek summary alert bhejein
            int count = pos.Count;
            string title = "Bulk PO Submitted";
            string message = $"{count} Purchase Orders have been submitted for your approval.";

            await _notificationRepository.AddNotificationAsync(
                title,
                message,
                "PO",
                "/app/inventory/polist", // Seedha list page par navigate karega
                pos.FirstOrDefault()?.BranchId,
                pos.FirstOrDefault()?.CompanyId ?? Guid.Empty
            );

            return true;
        }

        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="approvedBy"></param>
    /// <returns></returns>
    public async Task<bool> BulkApprovePOsAsync(List<Guid> ids, string approvedBy)
    {
        // 1. Fetch Submitted POs
        var pos = await _context.PurchaseOrders
            .Where(x => ids.Contains(x.Id) && x.Status == "Submitted")
            .ToListAsync();

        if (pos == null || !pos.Any()) return false;

        // 2. Status update to Approved
        foreach (var po in pos)
        {
            po.Status = "Approved";
            po.ModifiedBy = approvedBy;
            po.ModifiedOn = DateTime.Now;
        }

        // 3. Save Changes
        if (await _context.SaveChangesAsync() > 0)
        {
            // --- BULK APPROVAL NOTIFICATION TRIGGER ---
            int count = pos.Count;
            string title = "Bulk PO Approved";
            string message = $"{count} Purchase Orders have been approved and are ready for receipt.";

            await _notificationRepository.AddNotificationAsync(
                title,
                message,
                "PO",
                "/app/inventory/polist", // Redirect to PO list
                pos.FirstOrDefault()?.BranchId,
                pos.FirstOrDefault()?.CompanyId ?? Guid.Empty
            );

            // --- BULK EMAIL/WHATSAPP TRIGGER ---
            var poDataList = pos.Select(p => new { p.Id, p.PoNumber, p.GrandTotal, p.SupplierId }).ToList();
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                var companyClient = scope.ServiceProvider.GetRequiredService<ICompanyClient>();
                var supplierClient = scope.ServiceProvider.GetRequiredService<ISupplierClient>();

                try
                {
                    var company = await companyClient.GetCompanyProfileAsync();
                    if (company == null) return;

                    foreach (var poData in poDataList)
                    {
                        var supplier = await supplierClient.GetSupplierByIdAsync(poData.SupplierId);
                        if (supplier != null)
                        {
                            // 1. Email
                            if (!string.IsNullOrEmpty(supplier.Email))
                            {
                                await emailService.SendPoEmailAsync(company, supplier.Email, poData.PoNumber, poData.GrandTotal);
                            }

                            // 2. WhatsApp
                            if (!string.IsNullOrEmpty(supplier.Phone))
                            {
                                string msg = $"New Purchase Order from {company.Name}:\nPO Number: {poData.PoNumber}\nAmount: {poData.GrandTotal}\nPlease check your email for details.";
                                await whatsAppService.SendMessageAsync(supplier.Phone, msg);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PurchaseOrderRepository] Bulk Notification failed: {ex.Message}");
                }
            });

            return true;
        }

        return false;
    }

    /// <summary>
    /// bilk rejected
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="rejectedBy"></param>
    /// <returns></returns>
    public async Task<bool> BulkRejectPOsAsync(List<Guid> ids, string rejectedBy)
    {
        // 1. Sirf 'Submitted' status wale POs hi Reject kiye ja sakte hain
        var pos = await _context.PurchaseOrders
            .Where(x => ids.Contains(x.Id) && x.Status == "Submitted")
            .ToListAsync();

        if (pos == null || !pos.Any()) return false;

        foreach (var po in pos)
        {
            // 2. Status update to Rejected
            po.Status = "Rejected";
            po.ModifiedBy = rejectedBy;
            po.ModifiedOn = DateTime.Now;
        }

        // 3. Save Changes
        if (await _context.SaveChangesAsync() > 0)
        {
            // --- BULK REJECTION NOTIFICATION TRIGGER ---
            int count = pos.Count;
            string title = "Bulk PO Rejected";
            string message = $"{count} Purchase Orders have been rejected. Please check the list for details.";

            await _notificationRepository.AddNotificationAsync(
                title,
                message,
                "PO",
                "/app/inventory/polist", // Redirect to PO list to take action
                pos.FirstOrDefault()?.BranchId,
                pos.FirstOrDefault()?.CompanyId ?? Guid.Empty
            );

            return true;
        }

        return false;
    }

    public async Task<PODocumentDto> GetPODetailsForPrintAsync(Guid id)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        // 1. FIX: StringComparison ko hata kar simple '==' use karein taaki EF ise SQL mein translate kar sake
        // SQL Server default mein case-insensitive match hi karta hai
        bool isReceived = await _context.GRNHeaders
            .AsNoTracking()
            .AnyAsync(g => g.PurchaseOrderId == id && g.Status == "Received" && g.CompanyId == companyId);

        // 2. Data fetch karein optimized join ke saath
        return await _context.PurchaseOrders
            .AsNoTracking()
            .Where(x => x.Id == id && x.CompanyId == companyId)
            .Select(po => new PODocumentDto
            {
                // Header Mapping
                PoNumber = po.PoNumber,
                SupplierName = po.SupplierName,
                PoDate = po.PoDate,
                Remarks = po.Remarks,

                // Tax aur SubTotal details for Modal UI
                SubTotal = po.SubTotal,
                TotalTax = po.TotalTax,
                GrandTotal = po.GrandTotal,

                // Dynamic Title based on GRN status
                Status = isReceived ? "TAX INVOICE" : "BILL OF SUPPLY",
                CreatedBy = po.CreatedBy,

                // Items Mapping with Products Table Join
                Items = _context.PurchaseOrderItems
                    .AsNoTracking()
                    .Where(item => item.PurchaseOrderId == po.Id)
                    .Join(_context.Products,
                          item => item.ProductId,
                          prod => prod.Id,
                          (item, prod) => new POItemDocumentDto
                          {
                              ProductId = item.ProductId,
                              ProductName = prod.Name, // Actual Name
                              Qty = item.Qty,
                              Unit = item.Unit,
                              Rate = item.Rate,
                              DiscountPercent = item.DiscountPercent,
                              GstPercent = item.GstPercent,
                              TaxAmount = item.TaxAmount,
                              Total = item.Total,
                              MfgDate = item.MfgDate,
                              ExpDate = item.ExpDate
                          }).ToList()
            }).FirstOrDefaultAsync();
    }

    public async Task<PORepoPrintResponse> GeneratePOReportPdfAsync(Guid id)
    {
        // 1. Timeout Fix: Simple query bina nested include ke
        var po = await _context.PurchaseOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (po == null) return null;

        // 2. Fetch Company Profile [New Feature]
        CompanyProfileDto? companyProfile = null;
        try
        {
            companyProfile = await _companyClient.GetCompanyProfileAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching company profile: {ex.Message}");
        }

        string companyName = companyProfile?.Name ?? "ELECTRIC INVENTORY";
        string companyTagline = companyProfile?.Tagline ?? "PREMIUM INVENTORY MANAGEMENT SYSTEM";
        string companyLogoUrl = companyProfile?.LogoUrl;
        
        string companyAddress = "";
        if (companyProfile?.Address != null)
        {
            var addr = companyProfile.Address;
            companyAddress = $"{addr.AddressLine1}, {addr.City}, {addr.State} - {addr.PinCode}";
        }
        
        string contactInfo = $"Ph: {companyProfile?.PrimaryPhone} | Email: {companyProfile?.PrimaryEmail}";


        // 3. STATUS FIX: GRNHeaders table mein check karein ki kya ye PO receive ho chuka hai
        bool isReceived = await _context.GRNHeaders
            .AnyAsync(g => g.PurchaseOrderId == id && g.Status == "Received");

        string documentTitle = isReceived ? "TAX INVOICE" : "PURCHASE ORDER";

        // 4. Items fetch optimized
        var itemsWithNames = await (from item in _context.PurchaseOrderItems
                                    join prod in _context.Products on item.ProductId equals prod.Id
                                    where item.PurchaseOrderId == id
                                    select new
                                    {
                                        ProductName = prod.Name,
                                        item.Qty,
                                        item.Unit,
                                        item.Rate,
                                        item.Total,
                                        MfgDate = item.MfgDate,
                                        ExpDate = item.ExpDate,
                                        IsExpiryRequired = prod.IsExpiryRequired
                                    }).ToListAsync();

        // 5. HTML Template with dynamic header
        var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; padding: 30px; color: #333; line-height: 1.4; }}
        .header-container {{ display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #1a73e8; padding-bottom: 20px; margin-bottom: 20px; }}
        .company-details {{ float: left; }}
        .doc-title {{ float: right; text-align: right; }}
        
        .company-name {{ font-size: 24px; font-weight: bold; color: #1a73e8; margin: 0; }}
        .company-address {{ font-size: 13px; color: #555; margin: 2px 0; }}
        
        .bill-label {{ background: #f1f3f4; color: #333; padding: 5px 15px; border-radius: 4px; font-weight: bold; font-size: 18px; display: inline-block; }}
        
        .po-table {{ width: 100%; border-collapse: collapse; margin-top: 25px; }}
        .po-table th {{ background-color: #f8f9fa; border: 1px solid #dee2e6; padding: 12px; text-align: center; font-size: 14px; font-weight: 600; }}
        .po-table td {{ border: 1px solid #dee2e6; padding: 10px; font-size: 13px; }}
        .total-box {{ float: right; width: 40%; margin-top: 30px; border: none; }}
        
        /* Flexbox fixes for DinkToPdf (WebKit based) */
        .clearfix::after {{ content: ''; display: table; clear: both; }}
    </style>
</head>
<body>
    <div class='header-container clearfix'>
        <div class='company-details'>
            {(string.IsNullOrEmpty(companyLogoUrl) ? "" : $"<img src='{companyLogoUrl}' style='height: 60px; margin-bottom: 10px;' />")}
            <h1 class='company-name'>{companyName}</h1>
            <p class='company-address'>{companyTagline}</p>
            <p class='company-address'>{companyAddress}</p>
            <p class='company-address'>{contactInfo}</p>
        </div>
        <div class='doc-title'>
            <h2 class='bill-label'>{documentTitle}</h2>
            <p>PO #: {po.PoNumber}</p>
            <p>Date: {po.PoDate:dd MMM yyyy}</p>
        </div>
    </div>
    
    <table style='width: 100%; margin: 20px 0; border-spacing: 0;'>
        <tr>
            <td style='vertical-align: top; padding: 10px; background: #f9f9f9; border-radius: 5px; width: 48%;'>
                <strong style='color: #555; font-size: 12px; text-transform: uppercase;'>Vendor</strong><br/>
                <span style='font-size: 16px; font-weight: bold;'>{po.SupplierName}</span>
                <!-- Supplier Address could come here if available -->
            </td>
            <td style='width: 4%;'></td>
            <td style='vertical-align: top; padding: 10px; background: #f9f9f9; border-radius: 5px; width: 48%;'>
                 <!-- Optional: Ship To or Bill To details -->
                 <strong style='color: #555; font-size: 12px; text-transform: uppercase;'>Summary</strong><br/>
                 Expected Delivery: {po.ExpectedDeliveryDate:dd MMM yyyy}<br/>
                 Status: {po.Status}
            </td>
        </tr>
    </table>

    <table class='po-table'>
        <thead>
            <tr>
                <th style='text-align: left;'>Product Description</th>
                <th>Qty</th>
                <th>Unit</th>
                <th style='text-align: right;'>Rate</th>
                <th style='text-align: right;'>Total</th>
            </tr>
        </thead>
        <tbody>";

        foreach (var item in itemsWithNames)
        {
            htmlContent += $@"
        <tr>
            <td>
                <b>{item.ProductName}</b>
                {(item.IsExpiryRequired == true ? $"<br/><small style='color:#666'>Mfg: {(item.MfgDate.HasValue ? item.MfgDate.Value.ToString("dd/MM/yyyy") : "NA")} | Exp: {(item.ExpDate.HasValue ? item.ExpDate.Value.ToString("dd/MM/yyyy") : "NA")}</small>" : "")}
            </td>
            <td style='text-align: center;'>{item.Qty}</td>
            <td style='text-align: center;'>{item.Unit}</td>
            <td style='text-align: right;'>&#8377;{item.Rate:N2}</td>
            <td style='text-align: right;'>&#8377;{item.Total:N2}</td>
        </tr>";
        }

        htmlContent += $@"
        </tbody>
    </table>

    <div class='total-box'>
        <table style='width: 100%;'>
            <tr>
                <td style='padding: 5px 0;'><strong>Sub Total:</strong></td>
                <td style='text-align: right; padding: 5px 0;'>&#8377;{po.SubTotal:N2}</td>
            </tr>
            <tr>
                <td style='padding: 5px 0;'><strong>Tax:</strong></td>
                <td style='text-align: right; padding: 5px 0;'>&#8377;{po.TotalTax:N2}</td>
            </tr>
            <tr>
                <td style='border-top: 1px solid #ccc; padding-top: 10px;'><strong>Grand Total:</strong></td>
                <td style='text-align: right; color: #1a73e8; font-size: 1.3em; border-top: 1px solid #ccc; padding-top: 10px;'>
                    <strong>&#8377;{po.GrandTotal:N2}</strong>
                </td>
            </tr>
        </table>
    </div>
    
    <div style='margin-top: 100px; clear: both;'>
        <p style='border-top: 1px solid #333; width: 220px; text-align: center; font-weight: bold;'>Authorized Signatory</p>
        <p style='text-align: center; width: 220px; font-size: 12px;'>For {companyName}</p>
    </div>
</body>
</html>";

        // 6. PDF generation
        var pdfBytes = _converter.Convert(new HtmlToPdfDocument()
        {
            GlobalSettings = { PaperSize = PaperKind.A4, Margins = new MarginSettings { Top = 10, Bottom = 10 } },
            Objects = { new ObjectSettings { HtmlContent = htmlContent, WebSettings = { DefaultEncoding = "utf-8" } } }
        });

        return new PORepoPrintResponse
        {
            PdfBytes = pdfBytes,
            HeaderTitle = documentTitle
        };
    }

    public async Task<decimal> GetTotalReturnedQtyAsync(Guid poId)
    {
        // 1. Calculate Total Ordered
        var totalOrdered = await _context.PurchaseOrderItems
            .Where(x => x.PurchaseOrderId == poId)
            .SumAsync(x => x.Qty);

        // 2. Calculate Net Received (current warehouse stock from this PO)
        // Note: PurchaseReturnRepository subtracts ReturnedQty from ReceivedQty in GRNDetails
        var netReceived = await _context.GRNDetails
            .Where(gd => gd.GRNHeader.PurchaseOrderId == poId)
            .SumAsync(gd => gd.ReceivedQty);

        // 3. Difference (25 in user's case)
        var replacementNeeded = totalOrdered - netReceived;

        return replacementNeeded > 0 ? replacementNeeded : 0;
    }

    public async Task<bool> ToggleDispatchStatusAsync(Guid id)
    {
        var po = await _context.PurchaseOrders.FindAsync(id);
        if (po == null) return false;

        po.IsDispatched = !po.IsDispatched;
        po.ModifiedOn = DateTime.Now;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> BulkDeleteAsync(List<Guid> ids)
    {
        var pos = await _context.PurchaseOrders.Where(x => ids.Contains(x.Id)).ToListAsync();
        if(!pos.Any()) return false;
        
        _context.PurchaseOrders.RemoveRange(pos);
        return await _context.SaveChangesAsync() > 0;
    }
}
