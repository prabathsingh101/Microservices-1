using ClosedXML.Excel;
using Inventory.Application.Clients;
using Inventory.Application.PurchaseReturn;
using Inventory.Application.PurchaseReturn.DTOs;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Net.Http.Json;
using Inventory.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Inventory.Domain.Entities;
using System.Linq;

namespace Inventory.Infrastructure.Repositories;

public class PurchaseReturnRepository : Inventory.Application.Common.Interfaces.IPurchaseReturnRepository
{
    private readonly ICurrentUserService _currentUserService;
    private readonly InventoryDbContext _context;
    private readonly ISupplierClient _supplierClient;
    private readonly ICompanyClient _companyClient;

    public PurchaseReturnRepository(InventoryDbContext context, 
        ISupplierClient supplierClient,
        ICompanyClient companyClient,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _supplierClient = supplierClient;
        _companyClient = companyClient;
        _currentUserService = currentUserService;
    }

    // 1. UI Form ke liye Rejected Items fetch karein
    public async Task<List<RejectedItemDto>> GetRejectedItemsBySupplierAsync(Guid supplierId)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        var query = from gd in _context.GRNDetails
                         .Include(x => x.Product)
                         .Include(x => x.Warehouse)
                         .Include(x => x.Rack)
                    join gh in _context.GRNHeaders on gd.GRNHeaderId equals gh.Id
                    join po in _context.PurchaseOrders on gh.PurchaseOrderId equals po.Id into poGroup
                    from po in poGroup.DefaultIfEmpty()
                    where gh.SupplierId == supplierId && gd.RejectedQty > 0 && gh.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || gh.BranchId == branchId)
                    select new RejectedItemDto
                    {
                        ProductId = gd.ProductId,
                        ProductName = gd.Product != null ? gd.Product.Name : "Ukn-" + gd.ProductId.ToString().Substring(0,8),
                        Brand = gd.Product != null ? gd.Product.Brand : null,
                        Sku = gd.Product != null ? gd.Product.Sku : null,
                        GrnRef = gh.GRNNumber,
                        RejectedQty = gd.RejectedQty,
                        Rate = gd.UnitRate,
                        GstPercent = gd.GstPercent,
                        DiscountPercent = gd.DiscountPercent,
                        CurrentStock = (_context.GRNDetails.Where(g => g.ProductId == gd.ProductId && g.CompanyId == companyId && g.GRNHeader.Status != "Cancelled" && g.GRNHeader.Status != "Canceled").Sum(g => (decimal?)g.ReceivedQty - g.RejectedQty) ?? 0) - 
                                       (_context.SaleOrderItems.Where(si => si.ProductId == gd.ProductId && si.CompanyId == companyId && (si.SaleOrder.Status == "Confirmed" || si.SaleOrder.Status == "Completed")).Sum(si => (decimal?)si.Qty) ?? 0) +
                                       (_context.SaleReturnItems.Where(sri => sri.ProductId == gd.ProductId && sri.CompanyId == companyId && (sri.SaleReturnHeader.Status == "Confirmed" || sri.SaleReturnHeader.Status == "INWARDED" || sri.SaleReturnHeader.Status == "Refunded" || sri.SaleReturnHeader.Status == "Exchanged")).Sum(sri => (decimal?)sri.ReturnQty) ?? 0) -
                                       (_context.SaleExchangeItems.Where(sei => sei.ProductId == gd.ProductId && sei.CompanyId == companyId && (sei.SaleReturnHeader.Status == "Confirmed" || sei.SaleReturnHeader.Status == "INWARDED" || sei.SaleReturnHeader.Status == "Refunded" || sei.SaleReturnHeader.Status == "Exchanged")).Sum(sei => (decimal?)sei.Qty) ?? 0) -
                                       (_context.PurchaseReturnItems.Where(pri => pri.ProductId == gd.ProductId && pri.CompanyId == companyId && pri.PurchaseReturn.Status != "Cancelled" && pri.PurchaseReturn.Status != "Canceled").Sum(pri => (decimal?)pri.ReturnQty) ?? 0),
                        WarehouseName = gd.Warehouse != null ? gd.Warehouse.Name : "N/A",
                        RackName = gd.Rack != null ? gd.Rack.Name : "N/A",
                        WarehouseId = gd.WarehouseId,
                        RackId = gd.RackId,
                        MfgDate = gd.MfgDate,
                        ExpDate = gd.ExpDate,
                        BranchId = gh.BranchId,
                        IsSettled = gd.IsSettled,
                        PoNumber = po != null ? po.PoNumber : "Quick"
                    };

        return await query.ToListAsync();
    }

    public async Task<List<SupplierSelectDto>> GetSuppliersForPurchaseReturnAsync()
    {
        try
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;
            var allSupplierIds = await (from gh in _context.GRNHeaders
                                        where gh.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || gh.BranchId == branchId)
                                        select gh.SupplierId)
                                       .Distinct()
                                       .ToListAsync();

            if (allSupplierIds == null || !allSupplierIds.Any())
            {
                return new List<SupplierSelectDto>();
            }

            var suppliers = await _supplierClient.GetSuppliersByIdsAsync(allSupplierIds);
            return suppliers ?? new List<SupplierSelectDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetSuppliersForPurchaseReturnAsync: {ex.Message}");
        }
        return new List<SupplierSelectDto>();
    }

    public async Task<List<ReceivedStockDto>> GetReceivedStockBySupplierAsync(Guid supplierId)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        // 1. Fetch Company Profile for Return Policy [cite: 2026-04-08]
        var company = await _companyClient.GetCompanyProfileAsync();
        int windowValue = company?.PurchaseReturnWindowValue ?? 72;
        string windowUnit = company?.PurchaseReturnWindowUnit ?? "Hours";

        // 2. Calculate dynamic limit date
        double totalHours = windowUnit switch 
        {
            "Hours" => windowValue,
            "Days" => windowValue * 24,
            "Months" => windowValue * 30 * 24,
            _ => windowValue
        };
        
        var now = DateTime.Now;
        var limitDate = now.AddHours(-totalHours);

        var rawList = await (from gd in _context.GRNDetails
                        .Include(x => x.Product)
                        .Include(x => x.Warehouse)
                        .Include(x => x.Rack)
                    join gh in _context.GRNHeaders on gd.GRNHeaderId equals gh.Id
                    join po in _context.PurchaseOrders on gh.PurchaseOrderId equals po.Id into poGroup
                    from po in poGroup.DefaultIfEmpty()
                    where gh.SupplierId == supplierId && (gd.ReceivedQty - gd.RejectedQty) > 0 && gh.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || gh.BranchId == branchId)
                    select new { gd, gh, PoNumber = po != null ? po.PoNumber : "Quick" }).ToListAsync();

        var result = rawList.Select(x => {
            // Calculate already returned quantity for this specific Product and GRN [cite: 2026-05-04]
            var returnedQty = _context.PurchaseReturnItems
                .Where(pri => pri.ProductId == x.gd.ProductId && pri.GrnRef == x.gh.GRNNumber && pri.CompanyId == companyId)
                .Sum(pri => (decimal?)pri.ReturnQty) ?? 0;

            return new ReceivedStockDto
            {
                ProductId = x.gd.ProductId,
                ProductName = (x.gd.Product != null && !string.IsNullOrEmpty(x.gd.Product.Name)) ? x.gd.Product.Name : "Product-" + x.gd.ProductId.ToString().Substring(0, 8),
                Brand = x.gd.Product != null ? x.gd.Product.Brand : null,
                Sku = x.gd.Product != null ? x.gd.Product.Sku : null,
                GrnRef = x.gh.GRNNumber,
                AvailableQty = x.gd.ReceivedQty - x.gd.RejectedQty - returnedQty, // Subtracted returnedQty
                Rate = x.gd.UnitRate,
                GstPercent = x.gd.GstPercent,
                DiscountPercent = x.gd.DiscountPercent,
                ReceivedDate = x.gh.ReceivedDate,
                CurrentStock = (_context.GRNDetails.Where(g => g.ProductId == x.gd.ProductId && g.CompanyId == companyId && g.GRNHeader.Status != "Cancelled" && g.GRNHeader.Status != "Canceled").Sum(g => (decimal?)g.ReceivedQty - g.RejectedQty) ?? 0) - 
                               (_context.SaleOrderItems.Where(si => si.ProductId == x.gd.ProductId && si.CompanyId == companyId && (si.SaleOrder.Status == "Confirmed" || si.SaleOrder.Status == "Completed")).Sum(si => (decimal?)si.Qty) ?? 0) +
                               (_context.SaleReturnItems.Where(sri => sri.ProductId == x.gd.ProductId && sri.CompanyId == companyId && (sri.SaleReturnHeader.Status == "Confirmed" || sri.SaleReturnHeader.Status == "INWARDED" || sri.SaleReturnHeader.Status == "Refunded" || sri.SaleReturnHeader.Status == "Exchanged")).Sum(sri => (decimal?)sri.ReturnQty) ?? 0) -
                               (_context.SaleExchangeItems.Where(sei => sei.ProductId == x.gd.ProductId && sei.CompanyId == companyId && (sei.SaleReturnHeader.Status == "Confirmed" || sei.SaleReturnHeader.Status == "INWARDED" || sei.SaleReturnHeader.Status == "Refunded" || sei.SaleReturnHeader.Status == "Exchanged")).Sum(sei => (decimal?)sei.Qty) ?? 0) -
                               (_context.PurchaseReturnItems.Where(pri => pri.ProductId == x.gd.ProductId && pri.CompanyId == companyId && pri.PurchaseReturn.Status != "Cancelled" && pri.PurchaseReturn.Status != "Canceled").Sum(pri => (decimal?)pri.ReturnQty) ?? 0),
                WarehouseName = x.gd.Warehouse != null ? x.gd.Warehouse.Name : "N/A",
                RackName = x.gd.Rack != null ? x.gd.Rack.Name : "N/A",
                MfgDate = x.gd.MfgDate,
                ExpDate = x.gd.ExpDate,
                WarehouseId = x.gd.WarehouseId,
                RackId = x.gd.RackId,
                BranchId = x.gh.BranchId,
                IsReturnable = x.gh.ReceivedDate >= limitDate,
                RemainingHours = Math.Max(0, totalHours - (now - x.gh.ReceivedDate).TotalHours),
                PoNumber = x.PoNumber
            };
        }).Where(x => x.AvailableQty > 0)
        .OrderByDescending(x => x.ReceivedDate)
        .ThenByDescending(x => x.GrnRef)
        .ToList();

        return result;
    }

    public async Task<bool> CreatePurchaseReturnAsync(Inventory.Domain.Entities.PurchaseReturn returnData)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (returnData.Id == Guid.Empty) returnData.Id = Guid.NewGuid();
                returnData.ReturnNumber = $"PR-{DateTime.Now:yyyyMMddHHmmss}";

                decimal totalHeaderTax = 0;
                decimal totalHeaderSubTotal = 0;

                foreach (var item in returnData.Items)
                {
                    var companyId = returnData.CompanyId;
                    var branchId = returnData.BranchId;
                    var grnDetail = await _context.GRNDetails
                        .IgnoreQueryFilters() // 🚀 Super Admin bypass
                        .Include(gd => gd.GRNHeader)
                        .FirstOrDefaultAsync(gd => gd.ProductId == item.ProductId
                                             && gd.GRNHeader.GRNNumber == item.GrnRef
                                             && gd.CompanyId == companyId);

                    if (grnDetail == null) 
                    {
                        var contextBranch = _currentUserService.BranchId ?? "NULL";
                        throw new Exception($"GRN details not found for ProductId: {item.ProductId} and GrnRef: {item.GrnRef}. Search BranchId used: {branchId}, Session BranchId: {contextBranch}");
                    }

                    // Auto-resolve branchId if null (for Admin sessions) [cite: 2026-04-27]
                    if (string.IsNullOrEmpty(branchId))
                    {
                        branchId = grnDetail.BranchId;
                        if (string.IsNullOrEmpty(returnData.BranchId)) returnData.BranchId = branchId;
                    }
                    if (string.IsNullOrEmpty(item.BranchId)) item.BranchId = branchId;
                    if (item.CompanyId == Guid.Empty) item.CompanyId = companyId;
                    if (item.ReturnQty <= 0) continue;

                    var poItem = await _context.PurchaseOrderItems
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(poi => poi.ProductId == item.ProductId 
                                             && poi.PurchaseOrderId == grnDetail.GRNHeader.PurchaseOrderId
                                             && poi.CompanyId == companyId);

                    // If no PO, use DTO/Item values for calculation
                    if (poItem != null)
                    {
                        // ✅ DO NOT modify ReceivedQty - it is a historical record of physical gate entry.
                        // Returns are tracked separately in PurchaseReturnItems table.
                        // PendingQty = Ordered - (ReceivedQty - RejectedQty) handles this correctly:
                        //   e.g. Ordered=2, Received=2, Rejected=1, Returned=1
                        //   Net Accepted = 2 - 1 = 1, Pending = 2 - 1 = 1 (need 1 replacement) ✅

                        item.GstPercent = poItem.GstPercent;
                        item.DiscountPercent = poItem.DiscountPercent;
                        item.Rate = poItem.Rate;
                    }

                    decimal baseAmount = item.ReturnQty * item.Rate;
                    decimal discountAmt = baseAmount * (item.DiscountPercent / 100);
                    decimal taxableAmount = baseAmount - discountAmt;
                    decimal itemTax = taxableAmount * (item.GstPercent / 100);

                    item.TaxAmount = itemTax;
                    item.TotalAmount = taxableAmount + itemTax;

                    totalHeaderSubTotal += taxableAmount;
                    decimal initialRejectedQty = grnDetail.RejectedQty;
                    decimal qtyToReturn = item.ReturnQty;
                    totalHeaderTax += itemTax;

                    // 🚀 HISTORICAL INTEGRITY: We no longer modify GRNDetails (Received/Rejected) 
                    // during a return. The original GRN should remain a record of the inward gate entry.
                    // Net stock and Pending calculations now join with PurchaseReturnItems.
                    
                    // _context.GRNDetails.Update(grnDetail); // Removed to keep history intact

                    // 🚀 STOCK UPDATE: Only deduct stock for ACCEPTED items being returned.
                    // Rejected items are NOT added to WarehouseStocks during GRN (only net accepted is added),
                    // so returning a purely rejected item should NOT reduce WarehouseStocks.
                    // Formula: deduction = max(0, returnQty - rejectedQty)
                    // e.g. return 1 item that was rejected → deduction = max(0, 1-1) = 0 (correct)
                    // e.g. return 1 item that was accepted → deduction = max(0, 1-0) = 1 (correct)
                    decimal deductionFromCurrentStock = Math.Max(0, item.ReturnQty - initialRejectedQty);

                    var wsWarehouseId = item.WarehouseId ?? grnDetail.WarehouseId;
                    var wsRackId = item.RackId ?? grnDetail.RackId;

                    if (deductionFromCurrentStock > 0)
                    {
                        var warehouseStock = await _context.WarehouseStocks
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(ws => ws.ProductId == item.ProductId 
                                                 && ws.WarehouseId == wsWarehouseId
                                                 && ws.CompanyId == companyId);
                                                
                        if (warehouseStock != null)
                        {
                            warehouseStock.Quantity -= deductionFromCurrentStock;
                            if (warehouseStock.Quantity < 0) warehouseStock.Quantity = 0;
                            _context.WarehouseStocks.Update(warehouseStock);
                        }
                        else if (wsWarehouseId.HasValue)
                        {
                            await _context.WarehouseStocks.AddAsync(new WarehouseStock
                            {
                                ProductId = item.ProductId,
                                WarehouseId = wsWarehouseId.Value,
                                Quantity = -deductionFromCurrentStock,
                                CompanyId = companyId,
                                BranchId = branchId
                            });
                        }
                    }

                    // Always record inventory transaction for audit trail
                    var returnTx = new InventoryTransaction(
                        item.ProductId,
                        -item.ReturnQty,
                        returnData.IsQuick ? "QuickPurchaseReturn" : "PurchaseReturn",
                        returnData.ReturnNumber,
                        wsWarehouseId,
                        item.RackId ?? grnDetail.RackId,
                        item.MfgDate,
                        item.ExpDate,
                        companyId,
                        branchId
                    );
                    await _context.InventoryTransactions.AddAsync(returnTx);

                    // 🎯 SETTLE THE REJECTION: Mark the original GRNDetail as settled [cite: 2026-05-04]
                    if (grnDetail.RejectedQty > 0)
                    {
                        grnDetail.IsSettled = true;
                        _context.GRNDetails.Update(grnDetail);
                    }
                }

                returnData.SubTotal = totalHeaderSubTotal;
                returnData.TotalTax = totalHeaderTax;
                returnData.GrandTotal = totalHeaderSubTotal + totalHeaderTax;

                // 🎯 FIX: Calculate Financial Total for Ledger (Exclude Rejected Items)
                decimal totalFinancialGrandTotal = 0;
                foreach (var item in returnData.Items)
                {
                    var gd = await _context.GRNDetails
                        .IgnoreQueryFilters()
                        .Include(x => x.GRNHeader)
                        .FirstOrDefaultAsync(x => x.ProductId == item.ProductId && x.GRNHeader.GRNNumber == item.GrnRef && x.CompanyId == returnData.CompanyId);
                    
                    if (gd != null)
                    {
                        // Sirf un units ka paisa ledger me jayega jo pehle 'Accepted' thi
                        decimal financialQty = Math.Max(0, item.ReturnQty - gd.RejectedQty);
                        if (financialQty > 0)
                        {
                            decimal fBase = financialQty * item.Rate;
                            decimal fDisc = fBase * (item.DiscountPercent / 100);
                            decimal fTaxable = fBase - fDisc;
                            decimal fTax = fTaxable * (item.GstPercent / 100);
                            totalFinancialGrandTotal += (fTaxable + fTax);
                        }
                    }
                }

                _context.PurchaseReturns.Add(returnData);
                
                // ... (rest of the code for PO dispatch reset remains the same)
                var poIds = new List<Guid>();
                foreach (var item in returnData.Items)
                {
                    var poId = await _context.GRNDetails
                        .Include(gd => gd.GRNHeader)
                        .Where(gd => gd.ProductId == item.ProductId && gd.GRNHeader.GRNNumber == item.GrnRef)
                        .Select(gd => gd.GRNHeader.PurchaseOrderId)
                        .FirstOrDefaultAsync();
                    if (poId != Guid.Empty && !poIds.Contains(poId)) poIds.Add(poId);
                }

                foreach (var poId in poIds)
                {
                    var po = await _context.PurchaseOrders.FindAsync(poId);
                    if (po != null)
                    {
                        po.IsDispatched = false; 
                        _context.PurchaseOrders.Update(po);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                try 
                {
                   var sNameDict = await GetSupplierNamesFromMicroservice(new List<Guid> { returnData.SupplierId });
                   string sName = sNameDict.ContainsKey(returnData.SupplierId) ? sNameDict[returnData.SupplierId] : "Unknown";

                   // 🎯 BINGO: Use totalFinancialGrandTotal instead of GrandTotal
                   await _supplierClient.RecordPurchaseReturnAsync(
                       returnData.SupplierId, 
                       totalFinancialGrandTotal, 
                       returnData.ReturnNumber, 
                       $"Purchase Return: {returnData.ReturnNumber} ({sName})", 
                       _currentUserService.Email ?? "System"
                   );
                }
                catch (Exception ex) 
                { 
                    Console.WriteLine($"[PurchaseReturnRepository] Ledger sync failed: {ex.Message}");
                }

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var inner = ex.InnerException?.Message ?? "";
                Console.WriteLine($"PurchaseReturn Error: {ex.Message} | {inner}");
                throw new Exception($"Save failed: {ex.Message}. {inner}");
            }
        });
    }

    public async Task<PurchaseReturnPagedResponse> GetPurchaseReturnsAsync(
        string? search,
        int pageIndex,
        int pageSize,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? status = null,
        string? sortField = "ReturnDate",
        string? sortOrder = "desc",
        bool isQuick = false)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        var query = _context.PurchaseReturns.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsQuick == isQuick && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
            .AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(x => x.ReturnDate >= fromDate.Value);

        if (toDate.HasValue)
        {
            var endOfToDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.ReturnDate <= endOfToDate);
        }

        if (!string.IsNullOrEmpty(status))
        {
            if (status.ToUpper() == "TODAY")
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                query = query.Where(x => x.ReturnDate >= today && x.ReturnDate < tomorrow);
            }
            else if (status.ToUpper() == "CONFIRMED")
            {
                query = query.Where(x => x.Status == "Confirmed" && (x.GatePassNo == null || x.GatePassNo == ""));
            }
            else
            {
                query = query.Where(x => x.Status == status);
            }
        }

        if (!string.IsNullOrEmpty(search))
        {
            var s = search.ToLower().Trim();
            var matchedSupplierIds = await GetSupplierIdsByNameFromMicroservice(s);

            query = query.Where(x =>
                (x.ReturnNumber != null && x.ReturnNumber.ToLower().Contains(s)) ||
                (x.Remarks != null && x.Remarks.ToLower().Contains(s)) ||
                (matchedSupplierIds != null && matchedSupplierIds.Contains(x.SupplierId))
            );
        }

        var totalCount = await query.CountAsync();

        bool isDesc = sortOrder?.ToLower() == "desc" || string.IsNullOrEmpty(sortOrder);
        string effectiveSortField = sortField?.ToLower().Trim() switch
        {
            "totalamount" or "grandtotal" => "GrandTotal",
            "returnnumber" => "ReturnNumber",
            "returndate" => "ReturnDate",
            "id" => "Id",
            _ => "ReturnDate"
        };

        if (isDesc)
            query = query.OrderByDescending(x => EF.Property<object>(x, effectiveSortField)).ThenByDescending(x => x.ReturnDate);
        else
            query = query.OrderBy(x => EF.Property<object>(x, effectiveSortField)).ThenBy(x => x.ReturnDate);

        // 🎯 NEW DEFAULT: If sorting by ID (random Guid) or fallback, always prioritize latest records
        if (string.IsNullOrEmpty(sortField) || sortField.ToLower() == "id")
        {
            query = query.OrderByDescending(x => x.ReturnDate);
        }

        var pagedData = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync();

        if (pagedData == null || !pagedData.Any())
            return new PurchaseReturnPagedResponse { Items = new List<PurchaseReturnListDto>(), TotalCount = totalCount };

        var supplierIds = pagedData.Select(x => x.SupplierId).Distinct().ToList();
        var supplierNames = await GetSupplierNamesFromMicroservice(supplierIds);

        var pagedIds = pagedData.Select(x => x.Id).ToList();
        
        var returnItemsList = await (from ri in _context.PurchaseReturnItems.AsNoTracking()
                                     join p in _context.Products.AsNoTracking() on ri.ProductId equals p.Id
                                     where pagedIds.Contains(ri.PurchaseReturnId) && ri.CompanyId == companyId
                                     select new { ri.PurchaseReturnId, ri.GrnRef, ri.ReturnQty, ProductName = p.Name })
                                     .ToListAsync();

        var itemLookup = returnItemsList
            .GroupBy(x => x.PurchaseReturnId)
            .ToDictionary(g => g.Key, g => new {
                GrnRefs = string.Join(", ", g.Select(i => i.GrnRef).Distinct()),
                TotalQty = g.Sum(i => i.ReturnQty),
                ProductName = g.Count() == 1 ? g.First().ProductName : (g.Count() > 1 ? "Multiple Items" : "N/A")
            });

        var items = pagedData.Select(x => new PurchaseReturnListDto
        {
            Id = x.Id,
            ReturnNumber = x.ReturnNumber,
            ReturnDate = x.ReturnDate,
            SupplierName = supplierNames.GetValueOrDefault(x.SupplierId, "Unknown"),
            ProductName = itemLookup.ContainsKey(x.Id) ? itemLookup[x.Id].ProductName : "N/A",
            TotalQty = itemLookup.ContainsKey(x.Id) ? itemLookup[x.Id].TotalQty : 0,
            GrnRef = itemLookup.ContainsKey(x.Id) ? itemLookup[x.Id].GrnRefs : "N/A",
            TotalAmount = x.GrandTotal,
            Status = x.Status ?? "Completed",
            GatePassNo = x.GatePassNo,
            IsQuick = x.IsQuick
        }).ToList();

        return new PurchaseReturnPagedResponse { Items = items, TotalCount = totalCount };
    }

    private async Task<List<Guid>> GetSupplierIdsByNameFromMicroservice(string name)
    {
        return await _supplierClient.SearchSupplierIdsByNameAsync(name);
    }

    private async Task<Dictionary<Guid, string>> GetSupplierNamesFromMicroservice(List<Guid> supplierIds)
    {
        var dict = new Dictionary<Guid, string>();
        if (supplierIds == null || !supplierIds.Any()) return dict;

        try
        {
            var suppliers = await _supplierClient.GetSuppliersByIdsAsync(supplierIds);
            if (suppliers != null)
            {
                dict = suppliers.ToDictionary(x => x.Id, x => x.Name);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching supplier names: {ex.Message}");
        }

        return dict;
    }

    public async Task<PurchaseReturnDetailDto?> GetPurchaseReturnByIdAsync(Guid id)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        var purchaseReturn = await _context.PurchaseReturns
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId));

        if (purchaseReturn == null) return null;

        var itemDtos = await (from pri in _context.PurchaseReturnItems.AsNoTracking()
                              join p in _context.Products.AsNoTracking() on pri.ProductId equals p.Id
                              where pri.PurchaseReturnId == id && pri.CompanyId == companyId && p.CompanyId == companyId
                              select new PurchaseReturnItemDto
                              {
                                  ProductId = pri.ProductId,
                                  ProductName = p.Name,
                                  GrnRef = pri.GrnRef,
                                  ReturnQty = pri.ReturnQty,
                                  Rate = pri.Rate,
                                  GstPercent = pri.GstPercent,
                                  DiscountPercent = pri.DiscountPercent,
                                  TaxAmount = pri.TaxAmount,
                                  TotalAmount = pri.TotalAmount,
                                  MfgDate = pri.MfgDate ?? _context.GRNDetails.Where(g => g.ProductId == pri.ProductId && g.GRNHeader.GRNNumber == pri.GrnRef && g.CompanyId == companyId).Select(x => x.MfgDate).FirstOrDefault(),
                                  ExpDate = pri.ExpDate ?? _context.GRNDetails.Where(g => g.ProductId == pri.ProductId && g.GRNHeader.GRNNumber == pri.GrnRef && g.CompanyId == companyId).Select(x => x.ExpDate).FirstOrDefault(),
                                  IsExpiryRequired = p.IsExpiryRequired
                              }).ToListAsync();

        var supplierDict = await GetSupplierNamesFromMicroservice(new List<Guid> { purchaseReturn.SupplierId });
        string sName = supplierDict.ContainsKey(purchaseReturn.SupplierId)
                       ? supplierDict[purchaseReturn.SupplierId] : "Unknown";

        return new PurchaseReturnDetailDto
        {
            Id = purchaseReturn.Id,
            ReturnNumber = purchaseReturn.ReturnNumber,
            ReturnDate = purchaseReturn.ReturnDate,
            SupplierId = purchaseReturn.SupplierId,
            SupplierName = sName,
            Status = purchaseReturn.Status ?? "Completed",
            Remarks = purchaseReturn.Remarks,
            Items = itemDtos,
            IsQuick = purchaseReturn.IsQuick,
            SubTotal = purchaseReturn.SubTotal,
            TaxAmount = purchaseReturn.TotalTax,
            GrandTotal = purchaseReturn.GrandTotal
        };
    }

    public async Task<byte[]> ExportPurchaseReturnsToExcelAsync(DateTime? fromDate, DateTime? toDate)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        var data = await _context.PurchaseReturns
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId) &&
                        (!fromDate.HasValue || x.ReturnDate >= fromDate) &&
                        (!toDate.HasValue || x.ReturnDate <= toDate))
            .OrderByDescending(x => x.ReturnDate)
            .ToListAsync();

        var supplierIds = data.Select(x => x.SupplierId).Distinct().ToList();
        var supplierNamesDict = await GetSupplierNamesFromMicroservice(supplierIds);

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Debit Notes");
            string[] headers = { "Return #", "Date", "Supplier Name", "Sub Total", "Tax Amount", "Grand Total", "Remarks" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD");
                cell.Style.Font.FontColor = XLColor.White;
            }

            int currentRow = 2;
            foreach (var item in data)
            {
                worksheet.Cell(currentRow, 1).Value = item.ReturnNumber;
                worksheet.Cell(currentRow, 2).Value = item.ReturnDate.ToString("dd-MMM-yyyy");
                string sName = supplierNamesDict.ContainsKey(item.SupplierId)
                               ? supplierNamesDict[item.SupplierId]
                               : "Unknown Supplier";
                worksheet.Cell(currentRow, 3).Value = sName;
                worksheet.Cell(currentRow, 4).Value = item.SubTotal;
                worksheet.Cell(currentRow, 5).Value = item.TotalTax;
                worksheet.Cell(currentRow, 6).Value = item.GrandTotal;
                worksheet.Cell(currentRow, 7).Value = item.Remarks;
                worksheet.Range(currentRow, 4, currentRow, 6).Style.NumberFormat.Format = "₹ #,##0.00";
                currentRow++;
            }
            worksheet.Columns().AdjustToContents();
            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
        }
    }

    public async Task<List<PendingPRDto>> GetPendingPurchaseReturnsAsync()
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        var returns = await _context.PurchaseReturns
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.Status == "Confirmed" && (x.GatePassNo == null || x.GatePassNo == "") && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
            .OrderByDescending(x => x.ReturnDate)
            .Select(x => new PendingPRDto
            {
                Id = x.Id,
                ReturnNumber = x.ReturnNumber,
                ReturnDate = x.ReturnDate,
                Status = x.Status,
                SupplierId = x.SupplierId,
                TotalQty = x.Items.Sum(i => i.ReturnQty)
            })
            .ToListAsync();

        if (returns == null || !returns.Any()) return new List<PendingPRDto>();

        var supplierIds = returns.Select(r => r.SupplierId).Distinct().ToList();
        var supplierNames = await GetSupplierNamesFromMicroservice(supplierIds);

        foreach (var pr in returns)
        {
            if (supplierNames != null && supplierNames.TryGetValue(pr.SupplierId, out var name))
                pr.SupplierName = name;
            else
                pr.SupplierName = "Unknown Supplier";
        }
        return returns;
    }

    public async Task<bool> BulkOutwardAsync(List<Guid> ids)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        var records = await _context.PurchaseReturns
            .Where(x => ids.Contains(x.Id) && x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
            .ToListAsync();

        if (!records.Any()) return false;

        bool changed = false;
        foreach (var record in records)
        {
            if (record.Status != "OUTWARDED")
            {
                record.Status = "OUTWARDED";
                changed = true;
            }
        }

        if (changed)
        {
            await _context.SaveChangesAsync();
        }
        return true;
    }

    public async Task<PurchaseReturnSummaryDto> GetPurchaseReturnSummaryAsync(bool isQuick = false)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        var queryBase = _context.PurchaseReturns.Where(x => x.CompanyId == companyId && x.IsQuick == isQuick && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId));

        // 1. Aaj kitne returns aaye (Range check is safer than .Date)
        var totalToday = await queryBase.CountAsync(x => x.ReturnDate >= today && x.ReturnDate < tomorrow);
        
        // 2. Confirmed returns ka count aur refund value
        var confirmedQuery = queryBase.Where(x => x.Status == "Confirmed");
        var totalRefundValue = await confirmedQuery.SumAsync(x => (decimal?)x.GrandTotal) ?? 0;
        var confirmedCount = await confirmedQuery.CountAsync();
        
        // 3. Pending Outward Count (Confirmed but no GatePassNo) - Module specific
        var pendingOutwardCount = await queryBase.CountAsync(x => x.Status == "Confirmed" && (string.IsNullOrEmpty(x.GatePassNo)));
        
        // 4. Stock reduced pcs (Items table se sum)
        var totalPcs = await _context.PurchaseReturnItems
            .Where(x => x.CompanyId == companyId && x.PurchaseReturn.IsQuick == isQuick)
            .SumAsync(x => (decimal?)x.ReturnQty) ?? 0;

        return new PurchaseReturnSummaryDto
        {
            TotalReturnsToday = totalToday,
            TotalRefundValue = totalRefundValue,
            ConfirmedReturns = confirmedCount,
            PendingOutwardCount = pendingOutwardCount,
            StockReducedPcs = totalPcs
        };
    }

    public async Task<bool> CancelPurchaseReturnAsync(Guid id, string? reason)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var companyId = _currentUserService.CompanyId ?? Guid.Empty;
                var branchId = _currentUserService.BranchId;

                var purchaseReturn = await _context.PurchaseReturns
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId));

                if (purchaseReturn == null || purchaseReturn.Status == "Cancelled" || purchaseReturn.Status == "Canceled")
                {
                    return false;
                }

                if (purchaseReturn.Status == "Refund" || purchaseReturn.Status == "Replaced")
                {
                    throw new Exception($"This Purchase Return has already been {purchaseReturn.Status.ToLower()}ed and cannot be cancelled.");
                }

                // Check if it already has a Gate Pass outwarded. If so, can we cancel it?
                if (!string.IsNullOrEmpty(purchaseReturn.GatePassNo))
                {
                    throw new Exception("This Purchase Return has already been dispatched/outwarded and cannot be cancelled.");
                }

                foreach (var item in purchaseReturn.Items)
                {
                    // To find the original GRN detail for this product to see rejection info
                    var grnDetail = await _context.GRNDetails
                        .IgnoreQueryFilters()
                        .Include(gd => gd.GRNHeader)
                        .FirstOrDefaultAsync(gd => gd.ProductId == item.ProductId
                                             && gd.GRNHeader.GRNNumber == item.GrnRef
                                             && gd.CompanyId == companyId);

                    decimal initialRejectedQty = grnDetail?.RejectedQty ?? 0;
                    decimal deductionFromCurrentStock = Math.Max(0, item.ReturnQty - initialRejectedQty);

                    var wsWarehouseId = item.WarehouseId ?? grnDetail?.WarehouseId;

                    // Reversing the stock deduction: ADD stock back for accepted items that were returned
                    if (deductionFromCurrentStock > 0 && wsWarehouseId.HasValue)
                    {
                        var warehouseStock = await _context.WarehouseStocks
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(ws => ws.ProductId == item.ProductId 
                                                 && ws.WarehouseId == wsWarehouseId
                                                 && ws.CompanyId == companyId);
                                                
                        if (warehouseStock != null)
                        {
                            warehouseStock.Quantity += deductionFromCurrentStock;
                            _context.WarehouseStocks.Update(warehouseStock);
                        }
                    }

                    // Always record inventory transaction for audit trail (positive to add stock back)
                    var cancelTx = new InventoryTransaction(
                        item.ProductId,
                        item.ReturnQty, // Positive because we are RE-ADDING stock to inventory
                        (purchaseReturn.IsQuick ? "QuickPurchaseReturn" : "PurchaseReturn") + "-CANCELLED",
                        purchaseReturn.ReturnNumber,
                        wsWarehouseId,
                        item.RackId ?? grnDetail?.RackId,
                        item.MfgDate,
                        item.ExpDate,
                        companyId,
                        branchId
                    );
                    await _context.InventoryTransactions.AddAsync(cancelTx);

                    // Reset settle status on original GRN detail if it was marked settled
                    if (grnDetail != null && grnDetail.RejectedQty > 0)
                    {
                        var otherActiveReturnsExist = await _context.PurchaseReturnItems
                            .AnyAsync(ri => ri.Id != item.Id 
                                            && ri.GrnRef == item.GrnRef 
                                            && ri.ProductId == item.ProductId 
                                            && ri.PurchaseReturn.Status != "Cancelled" 
                                            && ri.PurchaseReturn.Status != "Canceled" 
                                            && ri.CompanyId == companyId);
                        if (!otherActiveReturnsExist)
                        {
                            grnDetail.IsSettled = false;
                            _context.GRNDetails.Update(grnDetail);
                        }
                    }
                }

                // Reverse the Ledger Entry (record a purchase/credit transaction to offset the return)
                decimal totalFinancialGrandTotal = 0;
                foreach (var item in purchaseReturn.Items)
                {
                    var gd = await _context.GRNDetails
                        .IgnoreQueryFilters()
                        .Include(x => x.GRNHeader)
                        .FirstOrDefaultAsync(x => x.ProductId == item.ProductId && x.GRNHeader.GRNNumber == item.GrnRef && x.CompanyId == companyId);
                    
                    if (gd != null)
                    {
                        decimal financialQty = Math.Max(0, item.ReturnQty - gd.RejectedQty);
                        if (financialQty > 0)
                        {
                            decimal fBase = financialQty * item.Rate;
                            decimal fDisc = fBase * (item.DiscountPercent / 100);
                            decimal fTaxable = fBase - fDisc;
                            decimal fTax = fTaxable * (item.GstPercent / 100);
                            totalFinancialGrandTotal += (fTaxable + fTax);
                        }
                    }
                }

                if (totalFinancialGrandTotal > 0)
                {
                    try 
                    {
                        var sNameDict = await GetSupplierNamesFromMicroservice(new List<Guid> { purchaseReturn.SupplierId });
                        string sName = sNameDict.ContainsKey(purchaseReturn.SupplierId) ? sNameDict[purchaseReturn.SupplierId] : "Unknown";

                        // Debit note reversal is recorded as a purchase transaction (Credit) to add back to the supplier balance
                        await _supplierClient.RecordPurchaseAsync(
                            purchaseReturn.SupplierId, 
                            totalFinancialGrandTotal, 
                            purchaseReturn.ReturnNumber, 
                            $"Reversal of Cancelled Purchase Return: {purchaseReturn.ReturnNumber} ({sName})", 
                            _currentUserService.Email ?? "System"
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ledger cancellation failed: {ex.Message}");
                    }
                }

                purchaseReturn.Status = "Cancelled";
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    purchaseReturn.Remarks = (purchaseReturn.Remarks ?? "") + $" | Cancelled Reason: {reason}";
                }
                purchaseReturn.ModifiedOn = DateTime.Now;
                purchaseReturn.ModifiedBy = _currentUserService.Email ?? "System";

                _context.PurchaseReturns.Update(purchaseReturn);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CancelPurchaseReturn] Error: {ex.Message}");
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<bool> CompleteRefundAsync(Guid? poId, string? poNumber, Guid? purchaseReturnId, string? purchaseReturnNumber)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var prsToUpdate = new List<Inventory.Domain.Entities.PurchaseReturn>();
                var companyId = _currentUserService.CompanyId ?? Guid.Empty;

                if (purchaseReturnId.HasValue && purchaseReturnId != Guid.Empty)
                {
                    var pr = await _context.PurchaseReturns.FirstOrDefaultAsync(x => x.Id == purchaseReturnId.Value && x.CompanyId == companyId);
                    if (pr != null) prsToUpdate.Add(pr);
                }
                else if (!string.IsNullOrEmpty(purchaseReturnNumber))
                {
                    var pr = await _context.PurchaseReturns.FirstOrDefaultAsync(x => x.ReturnNumber == purchaseReturnNumber && x.CompanyId == companyId);
                    if (pr != null) prsToUpdate.Add(pr);
                }
                else if (poId.HasValue && poId != Guid.Empty)
                {
                    var grnNumbers = await _context.GRNHeaders
                        .Where(g => g.PurchaseOrderId == poId.Value && g.CompanyId == companyId)
                        .Select(g => g.GRNNumber)
                        .ToListAsync();

                    if (grnNumbers.Any())
                    {
                        var prIds = await _context.PurchaseReturnItems
                            .Where(ri => grnNumbers.Contains(ri.GrnRef) && ri.CompanyId == companyId)
                            .Select(ri => ri.PurchaseReturnId)
                            .Distinct()
                            .ToListAsync();

                        prsToUpdate = await _context.PurchaseReturns
                            .Where(pr => prIds.Contains(pr.Id) && pr.Status == "Confirmed" && pr.CompanyId == companyId)
                            .ToListAsync();
                    }
                }
                else if (!string.IsNullOrEmpty(poNumber))
                {
                    var po = await _context.PurchaseOrders.FirstOrDefaultAsync(p => p.PoNumber == poNumber && p.CompanyId == companyId);
                    if (po != null)
                    {
                        var grnNumbers = await _context.GRNHeaders
                            .Where(g => g.PurchaseOrderId == po.Id && g.CompanyId == companyId)
                            .Select(g => g.GRNNumber)
                            .ToListAsync();

                        if (grnNumbers.Any())
                        {
                            var prIds = await _context.PurchaseReturnItems
                                .Where(ri => grnNumbers.Contains(ri.GrnRef) && ri.CompanyId == companyId)
                                .Select(ri => ri.PurchaseReturnId)
                                .Distinct()
                                .ToListAsync();

                            prsToUpdate = await _context.PurchaseReturns
                                .Where(pr => prIds.Contains(pr.Id) && pr.Status == "Confirmed" && pr.CompanyId == companyId)
                                .ToListAsync();
                        }
                    }
                }

                foreach (var pr in prsToUpdate)
                {
                    pr.Status = "Refund";
                    pr.ModifiedOn = DateTime.Now;
                    pr.ModifiedBy = _currentUserService.Email ?? "System";
                    _context.PurchaseReturns.Update(pr);
                }

                var poIdsToUpdate = new List<Guid>();
                if (poId.HasValue && poId != Guid.Empty)
                {
                    poIdsToUpdate.Add(poId.Value);
                }
                else
                {
                    var updatedPrIds = prsToUpdate.Select(p => p.Id).ToList();
                    if (updatedPrIds.Any())
                    {
                        var grnRefs = await _context.PurchaseReturnItems
                            .Where(ri => updatedPrIds.Contains(ri.PurchaseReturnId) && ri.CompanyId == companyId)
                            .Select(ri => ri.GrnRef)
                            .Distinct()
                            .ToListAsync();

                        if (grnRefs.Any())
                        {
                            var poIds = await _context.GRNHeaders
                                .Where(g => grnRefs.Contains(g.GRNNumber) && g.CompanyId == companyId)
                                .Select(g => g.PurchaseOrderId)
                                .Distinct()
                                .ToListAsync();

                            poIdsToUpdate.AddRange(poIds);
                        }
                    }
                }

                poIdsToUpdate = poIdsToUpdate.Distinct().ToList();

                foreach (var pId in poIdsToUpdate)
                {
                    var po = await _context.PurchaseOrders
                        .Include(p => p.Items)
                        .Include(p => p.GrnHeaders)
                        .ThenInclude(h => h.GRNItems)
                        .FirstOrDefaultAsync(p => p.Id == pId && p.CompanyId == companyId);

                    if (po != null)
                    {
                        var grnRefsOfPo = po.GrnHeaders.Select(g => g.GRNNumber).ToList();
                        var returnItemsOfPo = await _context.PurchaseReturnItems
                            .Include(ri => ri.PurchaseReturn)
                            .Where(ri => ri.PurchaseReturn.Status != "Cancelled" &&
                                         ri.PurchaseReturn.Status != "Canceled" &&
                                         ri.CompanyId == companyId &&
                                         grnRefsOfPo.Contains(ri.GrnRef))
                            .ToListAsync();

                        po.TotalRejected = po.GrnHeaders
                            .SelectMany(h => h.GRNItems ?? new List<GRNDetail>())
                            .Where(gd => !gd.IsSettled)
                            .Sum(gd => gd.RejectedQty);

                        po.TotalReturned = returnItemsOfPo.Sum(ri => ri.ReturnQty);
                        decimal totalRefunded = returnItemsOfPo
                            .Where(ri => ri.PurchaseReturn.Status == "Refund")
                            .Sum(ri => ri.ReturnQty);

                        decimal totalOrdered = po.Items.Sum(x => x.Qty);
                        decimal totalReceived = po.Items.Sum(x => x.ReceivedQty);
                        decimal totalAccepted = totalReceived - po.TotalRejected - po.TotalReturned;
                        decimal totalPending = Math.Max(0, totalOrdered - totalAccepted - totalRefunded);

                        if (totalPending == 0)
                        {
                            po.Status = totalRefunded > 0 ? "ShortClosed" : "Closed";
                        }
                        else
                        {
                            po.Status = "Partially Received";
                        }

                        po.ModifiedOn = DateTime.Now;
                        po.ModifiedBy = _currentUserService.Email ?? "System";
                        _context.PurchaseOrders.Update(po);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error in CompleteRefundAsync: {ex.Message}");
                throw;
            }
        });
    }

    public async Task<List<PoPendingRefundDto>> GetPendingRefundsByPoIdAsync(Guid poId)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;

        // 1. Get all GRN Numbers for the PO
        var grnNumbers = await _context.GRNHeaders
            .Where(g => g.PurchaseOrderId == poId && g.CompanyId == companyId)
            .Select(g => g.GRNNumber)
            .ToListAsync();

        if (!grnNumbers.Any())
        {
            return new List<PoPendingRefundDto>();
        }

        // 2. Find all Confirmed Purchase Returns that contain items referencing these GRNs
        var prs = await _context.PurchaseReturns
            .Include(pr => pr.Items)
            .Where(pr => pr.Status == "Confirmed" &&
                         pr.CompanyId == companyId &&
                         pr.Items.Any(item => grnNumbers.Contains(item.GrnRef)))
            .ToListAsync();

        var result = new List<PoPendingRefundDto>();

        foreach (var pr in prs)
        {
            var dto = new PoPendingRefundDto
            {
                PurchaseReturnId = pr.Id,
                ReturnNumber = pr.ReturnNumber,
                ReturnDate = pr.ReturnDate,
                Remarks = pr.Remarks ?? ""
            };

            foreach (var item in pr.Items)
            {
                // Only include item if it belongs to this PO's GRNs
                if (grnNumbers.Contains(item.GrnRef))
                {
                    // Fetch product name
                    var product = await _context.Products
                        .Where(p => p.Id == item.ProductId && p.CompanyId == companyId)
                        .Select(p => p.Name)
                        .FirstOrDefaultAsync();

                    dto.Items.Add(new PoPendingRefundItemDto
                    {
                        PurchaseReturnId = pr.Id,
                        PurchaseReturnNumber = pr.ReturnNumber,
                        ProductId = item.ProductId,
                        ProductName = product ?? "N/A",
                        ReturnQty = item.ReturnQty,
                        Rate = item.Rate,
                        GstPercent = item.GstPercent,
                        DiscountPercent = item.DiscountPercent,
                        TotalAmount = item.TotalAmount
                    });
                }
            }

            if (dto.Items.Any())
            {
                result.Add(dto);
            }
        }

        return result;
    }
}

