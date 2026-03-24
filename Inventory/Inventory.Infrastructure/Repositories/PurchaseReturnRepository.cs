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

namespace Inventory.Infrastructure.Repositories;

public class PurchaseReturnRepository : Inventory.Application.Common.Interfaces.IPurchaseReturnRepository
{
    private readonly InventoryDbContext _context;
    private readonly ISupplierClient _supplierClient;

    public PurchaseReturnRepository(InventoryDbContext context, 
        ISupplierClient supplierClient)
    {
        _context = context;
        _supplierClient = supplierClient;
    }

    // 1. UI Form ke liye Rejected Items fetch karein
    public async Task<List<RejectedItemDto>> GetRejectedItemsBySupplierAsync(int supplierId)
    {
        // Robusted query: Fetching directly from GRN details to avoid join failures with PO [cite: PR List Fix]
        var query = from gd in _context.GRNDetails
                        .Include(x => x.Product)
                        .Include(x => x.Warehouse)
                        .Include(x => x.Rack)
                    join gh in _context.GRNHeaders on gd.GRNHeaderId equals gh.Id
                    where gh.SupplierId == supplierId && gd.RejectedQty > 0
                    select new RejectedItemDto
                    {
                        ProductId = gd.ProductId,
                        ProductName = gd.Product != null ? gd.Product.Name : "Ukn-" + gd.ProductId.ToString().Substring(0,8),
                        GrnRef = gh.GRNNumber,
                        RejectedQty = gd.RejectedQty,
                        Rate = gd.UnitRate, // Using rate from GRN directly
                        GstPercent = gd.GstPercent,
                        DiscountPercent = gd.DiscountPercent,
                        CurrentStock = gd.Product != null ? gd.Product.CurrentStock : 0,
                        WarehouseName = gd.Warehouse != null ? gd.Warehouse.Name : "N/A",
                        RackName = gd.Rack != null ? gd.Rack.Name : "N/A",
                        MfgDate = gd.MfgDate,
                        ExpDate = gd.ExpDate
                    };

        return await query.ToListAsync();
    }

    public async Task<List<SupplierSelectDto>> GetSuppliersForPurchaseReturnAsync()
    {
        try
        {
            var allSupplierIds = await (from gh in _context.GRNHeaders
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

    public async Task<List<ReceivedStockDto>> GetReceivedStockBySupplierAsync(int supplierId)
    {
        // accepted stock fetch [cite: PR List Fix]
        var query = from gd in _context.GRNDetails
                        .Include(x => x.Product)
                        .Include(x => x.Warehouse)
                        .Include(x => x.Rack)
                    join gh in _context.GRNHeaders on gd.GRNHeaderId equals gh.Id
                    where gh.SupplierId == supplierId && (gd.ReceivedQty - gd.RejectedQty) > 0
                    select new ReceivedStockDto
                    {
                        ProductId = gd.ProductId,
                        ProductName = (gd.Product != null && !string.IsNullOrEmpty(gd.Product.Name)) ? gd.Product.Name : "Product-" + gd.ProductId.ToString().Substring(0, 8),
                        GrnRef = gh.GRNNumber,
                        AvailableQty = gd.ReceivedQty - gd.RejectedQty,
                        Rate = gd.UnitRate, // Using rate from GRN directly
                        GstPercent = gd.GstPercent,
                        DiscountPercent = gd.DiscountPercent,
                        ReceivedDate = gh.ReceivedDate,
                        CurrentStock = gd.Product != null ? gd.Product.CurrentStock : 0,
                        WarehouseName = gd.Warehouse != null ? gd.Warehouse.Name : "N/A",
                        RackName = gd.Rack != null ? gd.Rack.Name : "N/A",
                        MfgDate = gd.MfgDate,
                        ExpDate = gd.ExpDate
                    };

        var result = await query
            .OrderByDescending(x => x.ReceivedDate)
            .ThenByDescending(x => x.GrnRef)
            .ToListAsync();

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
                    var grnDetail = await _context.GRNDetails
                        .Include(gd => gd.GRNHeader)
                        .FirstOrDefaultAsync(gd => gd.ProductId == item.ProductId
                                             && gd.GRNHeader.GRNNumber == item.GrnRef);

                    if (grnDetail == null) throw new Exception($"GRN not found for {item.GrnRef}");
                    if (item.ReturnQty <= 0) continue;

                    var poItem = await _context.PurchaseOrderItems
                        .FirstOrDefaultAsync(poi => poi.ProductId == item.ProductId 
                                             && poi.PurchaseOrderId == grnDetail.GRNHeader.PurchaseOrderId);

                    if (poItem != null)
                    {
                        poItem.ReceivedQty -= item.ReturnQty;
                        if (poItem.ReceivedQty < 0) poItem.ReceivedQty = 0;
                        _context.PurchaseOrderItems.Update(poItem);

                        item.GstPercent = poItem.GstPercent;
                        item.DiscountPercent = poItem.DiscountPercent;
                        item.Rate = poItem.Rate;

                        decimal baseAmount = item.ReturnQty * item.Rate;
                        decimal discountAmt = baseAmount * (item.DiscountPercent / 100);
                        decimal taxableAmount = baseAmount - discountAmt;
                        decimal itemTax = taxableAmount * (item.GstPercent / 100);

                        item.TaxAmount = itemTax;
                        item.TotalAmount = taxableAmount + itemTax;

                        totalHeaderSubTotal += taxableAmount;
                        totalHeaderTax += itemTax;
                    }

                    // Stock Update Logic: 
                    // Calculate how much we are taking from 'Accepted' (CurrentStock) vs 'Rejected' bucket
                    decimal initialRejectedQty = grnDetail.RejectedQty;
                    decimal qtyToReturn = item.ReturnQty;

                    // 1. Update GRN Detail counts (Deducting from Total Received)
                    if (grnDetail.RejectedQty >= qtyToReturn)
                    {
                        grnDetail.RejectedQty -= qtyToReturn;
                    }
                    else
                    {
                        grnDetail.RejectedQty = 0;
                    }

                    grnDetail.ReceivedQty -= qtyToReturn;
                    if (grnDetail.ReceivedQty < 0) grnDetail.ReceivedQty = 0;
                    grnDetail.AcceptedQty = grnDetail.ReceivedQty - grnDetail.RejectedQty;
                    if (grnDetail.AcceptedQty < 0) grnDetail.AcceptedQty = 0;

                    _context.GRNDetails.Update(grnDetail);

                    // 2. Update Product Master Stock
                    // CurrentStock in Product table ONLY tracks Accepted (sellable) items.
                    // If we return items that were previously rejected, they don't affect CurrentStock.
                    decimal deductionFromCurrentStock = Math.Max(0, qtyToReturn - initialRejectedQty);

                    if (deductionFromCurrentStock > 0)
                    {
                        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
                        if (product != null)
                        {
                            product.CurrentStock -= deductionFromCurrentStock;
                            _context.Products.Update(product);

                            // 🆕 Record Inventory Transaction
                            var returnTx = new InventoryTransaction(
                                item.ProductId,
                                -item.ReturnQty, // Negative because it is REDUCING stock (returning to supplier)
                                returnData.IsQuick ? "QuickPurchaseReturn" : "PurchaseReturn",
                                returnData.ReturnNumber,
                                grnDetail.WarehouseId,
                                grnDetail.RackId,
                                item.MfgDate,
                                item.ExpDate
                            );
                            await _context.InventoryTransactions.AddAsync(returnTx);
                        }
                    }
                } // End Foreach

                returnData.SubTotal = totalHeaderSubTotal;
                returnData.TotalTax = totalHeaderTax;
                returnData.GrandTotal = totalHeaderSubTotal + totalHeaderTax;

                _context.PurchaseReturns.Add(returnData);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                try 
                {
                   await _supplierClient.RecordPurchaseReturnAsync(
                       returnData.SupplierId, 
                       returnData.GrandTotal, 
                       returnData.ReturnNumber, 
                       $"Purchase Return: {returnData.ReturnNumber}", 
                       "System"
                   );
                }
                catch { /* Log if needed */ }

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error: {ex.Message}");
                return false;
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
        // 1. Initial Query with NoTracking for high performance
        var query = _context.PurchaseReturns
            .AsNoTracking()
            .AsQueryable();

        if (isQuick)
        {
            query = query.Where(x => x.IsQuick == true);
        }

        // 2. Date Filtering Logic
        if (fromDate.HasValue)
            query = query.Where(x => x.ReturnDate >= fromDate.Value);

        if (toDate.HasValue)
        {
            var endOfToDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.ReturnDate <= endOfToDate);
        }

        // 2.5 Status Filter Logic [cite: 2026-02-23]
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

        // 3. Robust Searching Logic
        if (!string.IsNullOrEmpty(search))
        {
            var s = search.ToLower().Trim();

            // Step A: Microservice se matching Supplier IDs fetch karein
            var matchedSupplierIds = await GetSupplierIdsByNameFromMicroservice(s);

            // Step B: Search by ReturnNumber, Remarks, or Supplier
            query = query.Where(x =>
                (x.ReturnNumber != null && x.ReturnNumber.ToLower().Contains(s)) ||
                (x.Remarks != null && x.Remarks.ToLower().Contains(s)) ||
                (matchedSupplierIds != null && matchedSupplierIds.Contains(x.SupplierId))
            );
        }

        // 4. Server-side Count (Fast performance)
        var totalCount = await query.CountAsync();

        // 5. SORTING LOGIC: Mapping UI fields to DB columns
        bool isDesc = sortOrder?.ToLower() == "desc" || string.IsNullOrEmpty(sortOrder);
        string effectiveSortField = sortField?.ToLower().Trim() switch
        {
            "totalamount" or "grandtotal" => "GrandTotal",
            "returnnumber" => "ReturnNumber",
            "returndate" => "ReturnDate",
            "id" => "Id",
            _ => "ReturnDate" // Default: ReturnDate load orders by date
        };

        if (isDesc)
            query = query.OrderByDescending(x => EF.Property<object>(x, effectiveSortField));
        else
            query = query.OrderBy(x => EF.Property<object>(x, effectiveSortField));

        // 6. Execution & Pagination
        var pagedData = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync();

        if (pagedData == null || !pagedData.Any())
            return new PurchaseReturnPagedResponse { Items = new List<PurchaseReturnListDto>(), TotalCount = totalCount };

        // 7. Bulk Data Enrichment (Supplier Names & Items)
        var supplierIds = pagedData.Select(x => (long)x.SupplierId).Distinct().ToList();
        var supplierNames = await GetSupplierNamesFromMicroservice(supplierIds);

        var pagedIds = pagedData.Select(x => x.Id).ToList();
        
        // Items enrichment using Split Query pattern for efficiency
        var grnDetailsList = await _context.PurchaseReturnItems
            .AsNoTracking()
            .Where(ri => pagedIds.Contains(ri.PurchaseReturnId))
            .Select(ri => new { ri.PurchaseReturnId, ri.GrnRef })
            .Distinct()
            .ToListAsync();

        var grnLookup = grnDetailsList
            .GroupBy(x => x.PurchaseReturnId)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(i => i.GrnRef).Distinct()));

        // 8. Final Mapping
        var items = pagedData.Select(x => new PurchaseReturnListDto
        {
            Id = x.Id,
            ReturnNumber = x.ReturnNumber,
            ReturnDate = x.ReturnDate,
            SupplierName = supplierNames.GetValueOrDefault((long)x.SupplierId, "Unknown"),
            GrnRef = grnLookup.GetValueOrDefault(x.Id, "N/A"),
            TotalAmount = x.GrandTotal,
            Status = "Completed",
            GatePassNo = x.GatePassNo,
            IsQuick = x.IsQuick
        }).ToList();

        return new PurchaseReturnPagedResponse { Items = items, TotalCount = totalCount };
    }

    // 9. Helper Method to fetch matching Supplier IDs [cite: 2026-02-04]
    private async Task<List<int>> GetSupplierIdsByNameFromMicroservice(string name)
    {
        return await _supplierClient.SearchSupplierIdsByNameAsync(name);
    }




    private async Task<Dictionary<long, string>> GetSupplierNamesFromMicroservice(List<long> supplierIds)
    {
        var dict = new Dictionary<long, string>();
        if (supplierIds == null || !supplierIds.Any()) return dict;

        try
        {
            var suppliers = await _supplierClient.GetSuppliersByIdsAsync(supplierIds.Select(x => (int)x).ToList());
            if (suppliers != null)
            {
                dict = suppliers.ToDictionary(x => (long)x.Id, x => x.Name);
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
        // 1. Optimize: Eager Loading use karein aur database par ek hi lightweight call bhein
        var purchaseReturn = await _context.PurchaseReturns
            .AsNoTracking() // Read-only query ke liye best performance
            .Include(x => x.Items) // Navigation property se items fetch karein
            .FirstOrDefaultAsync(x => x.Id == id);

        if (purchaseReturn == null) return null;

        // 2. Optimization: Items aur Products ka mapping database level ki jagah memory mein karein
        // Taaki nested join ka timeout load khatam ho jaye
        var itemDtos = await (from pri in _context.PurchaseReturnItems.AsNoTracking()
                              join p in _context.Products.AsNoTracking() on pri.ProductId equals p.Id
                              where pri.PurchaseReturnId == id
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
                                  // Backfill from original GRN if not saved directly in PR record [cite: 2026-03-24]
                                  MfgDate = pri.MfgDate ?? _context.GRNDetails.Where(g => g.ProductId == pri.ProductId && g.GRNHeader.GRNNumber == pri.GrnRef).Select(x => x.MfgDate).FirstOrDefault(),
                                  ExpDate = pri.ExpDate ?? _context.GRNDetails.Where(g => g.ProductId == pri.ProductId && g.GRNHeader.GRNNumber == pri.GrnRef).Select(x => x.ExpDate).FirstOrDefault()
                              }).ToListAsync();

        // 3. Supplier Name fetch karein
        var supplierDict = await GetSupplierNamesFromMicroservice(new List<long> { (long)purchaseReturn.SupplierId });
        string sName = supplierDict.ContainsKey((long)purchaseReturn.SupplierId)
                       ? supplierDict[(long)purchaseReturn.SupplierId] : "Unknown";

        // 4. Final DTO Mapping (No functional changes, purely performance fix)
        return new PurchaseReturnDetailDto
        {
            Id = purchaseReturn.Id,
            ReturnNumber = purchaseReturn.ReturnNumber,
            ReturnDate = purchaseReturn.ReturnDate,
            SupplierId = purchaseReturn.SupplierId,
            SupplierName = sName,
            Status = "Completed", // Existing requirement status fix
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
        // 1. Database se Purchase Returns fetch karein [cite: 2026-02-04]
        var data = await _context.PurchaseReturns
            .AsNoTracking()
            .Where(x => (!fromDate.HasValue || x.ReturnDate >= fromDate) &&
                        (!toDate.HasValue || x.ReturnDate <= toDate))
            .OrderByDescending(x => x.ReturnDate)
            .ToListAsync();

        // 2. Microservice se Supplier Names fetch karein [cite: 2026-02-04]
        var supplierIds = data.Select(x => (long)x.SupplierId).Distinct().ToList();
        var supplierNamesDict = await GetSupplierNamesFromMicroservice(supplierIds); // Aapka existing helper method

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Debit Notes");

            // Headers definition [cite: 2026-02-04]
            string[] headers = { "Return #", "Date", "Supplier Name", "Sub Total", "Tax Amount", "Grand Total", "Remarks" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD");
                cell.Style.Font.FontColor = XLColor.White;
            }

            // 3. Fill Data Rows with Supplier Names [cite: 2026-02-04]
            int currentRow = 2;
            foreach (var item in data)
            {
                worksheet.Cell(currentRow, 1).Value = item.ReturnNumber;
                worksheet.Cell(currentRow, 2).Value = item.ReturnDate.ToString("dd-MMM-yyyy");

                // --- FIX: Microservice dictionary se naam map karein --- [cite: 2026-02-04]
                string sName = supplierNamesDict.ContainsKey((long)item.SupplierId)
                               ? supplierNamesDict[(long)item.SupplierId]
                               : "Unknown Supplier";
                worksheet.Cell(currentRow, 3).Value = sName;

                worksheet.Cell(currentRow, 4).Value = item.SubTotal;
                worksheet.Cell(currentRow, 5).Value = item.TotalTax;
                worksheet.Cell(currentRow, 6).Value = item.GrandTotal;
                worksheet.Cell(currentRow, 7).Value = item.Remarks;

                // Formatting [cite: 2026-02-04]
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
        var returns = await _context.PurchaseReturns
            .AsNoTracking()
            .Where(x => x.Status == "Confirmed" && (x.GatePassNo == null || x.GatePassNo == ""))
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

        var supplierIds = returns.Select(r => (long)r.SupplierId).Distinct().ToList();
        var supplierNames = await GetSupplierNamesFromMicroservice(supplierIds);

        foreach (var pr in returns)
        {
            if (supplierNames != null && supplierNames.TryGetValue((long)pr.SupplierId, out var name))
                pr.SupplierName = name;
            else
                pr.SupplierName = "Unknown Supplier";
        }

        return returns;
    }

    public async Task<bool> BulkOutwardAsync(List<Guid> ids)
    {
        var records = await _context.PurchaseReturns
            .Where(x => ids.Contains(x.Id))
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

    public async Task<PurchaseReturnSummaryDto> GetPurchaseReturnSummaryAsync()
    {
        var today = DateTime.Today;

        // 1. Aaj kitne returns huye
        var totalToday = await _context.PurchaseReturns
            .CountAsync(x => x.ReturnDate.Date == today);

        // 2. Confirmed returns ka count aur refund value
        var confirmedQuery = _context.PurchaseReturns
            .Where(x => x.Status == "Confirmed");

        var totalRefundValue = await confirmedQuery.SumAsync(x => x.GrandTotal);
        var confirmedCount = await confirmedQuery.CountAsync();

        // 3. Pending Outward Count (Confirmed but no GatePassNo)
        var pendingOutwardCount = await _context.PurchaseReturns
            .CountAsync(x => x.Status == "Confirmed" && (x.GatePassNo == null || x.GatePassNo == ""));

        // 4. Stock reduced pcs (Items table se sum)
        var totalPcs = await _context.PurchaseReturnItems
            .SumAsync(x => x.ReturnQty);

        return new PurchaseReturnSummaryDto
        {
            TotalReturnsToday = totalToday,
            TotalRefundValue = totalRefundValue,
            ConfirmedReturns = confirmedCount,
            PendingOutwardCount = pendingOutwardCount,
            StockReducedPcs = totalPcs
        };
    }
}
