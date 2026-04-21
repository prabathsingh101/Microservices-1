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
        var query = from gd in _context.GRNDetails
                         .Include(x => x.Product)
                         .Include(x => x.Warehouse)
                         .Include(x => x.Rack)
                    join gh in _context.GRNHeaders on gd.GRNHeaderId equals gh.Id
                    where gh.SupplierId == supplierId && gd.RejectedQty > 0 && gh.CompanyId == companyId
                    select new RejectedItemDto
                    {
                        ProductId = gd.ProductId,
                        ProductName = gd.Product != null ? gd.Product.Name : "Ukn-" + gd.ProductId.ToString().Substring(0,8),
                        GrnRef = gh.GRNNumber,
                        RejectedQty = gd.RejectedQty,
                        Rate = gd.UnitRate,
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
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var allSupplierIds = await (from gh in _context.GRNHeaders
                                        where gh.CompanyId == companyId
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
                    where gh.SupplierId == supplierId && (gd.ReceivedQty - gd.RejectedQty) > 0 && gh.CompanyId == companyId
                    select new { gd, gh }).ToListAsync();

        var result = rawList.Select(x => new ReceivedStockDto
        {
            ProductId = x.gd.ProductId,
            ProductName = (x.gd.Product != null && !string.IsNullOrEmpty(x.gd.Product.Name)) ? x.gd.Product.Name : "Product-" + x.gd.ProductId.ToString().Substring(0, 8),
            GrnRef = x.gh.GRNNumber,
            AvailableQty = x.gd.ReceivedQty - x.gd.RejectedQty,
            Rate = x.gd.UnitRate,
            GstPercent = x.gd.GstPercent,
            DiscountPercent = x.gd.DiscountPercent,
            ReceivedDate = x.gh.ReceivedDate,
            CurrentStock = x.gd.Product != null ? x.gd.Product.CurrentStock : 0,
            WarehouseName = x.gd.Warehouse != null ? x.gd.Warehouse.Name : "N/A",
            RackName = x.gd.Rack != null ? x.gd.Rack.Name : "N/A",
            MfgDate = x.gd.MfgDate,
            ExpDate = x.gd.ExpDate,
            IsReturnable = x.gh.ReceivedDate >= limitDate,
            RemainingHours = Math.Max(0, totalHours - (now - x.gh.ReceivedDate).TotalHours)
        })
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
                    var grnDetail = await _context.GRNDetails
                        .Include(gd => gd.GRNHeader)
                        .FirstOrDefaultAsync(gd => gd.ProductId == item.ProductId
                                             && gd.GRNHeader.GRNNumber == item.GrnRef
                                             && gd.CompanyId == companyId);

                    if (grnDetail == null) throw new Exception($"GRN not found for {item.GrnRef}");
                    if (item.ReturnQty <= 0) continue;

                    var poItem = await _context.PurchaseOrderItems
                        .FirstOrDefaultAsync(poi => poi.ProductId == item.ProductId 
                                             && poi.PurchaseOrderId == grnDetail.GRNHeader.PurchaseOrderId
                                             && poi.CompanyId == companyId);

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

                    decimal initialRejectedQty = grnDetail.RejectedQty;
                    decimal qtyToReturn = item.ReturnQty;

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

                    decimal deductionFromCurrentStock = Math.Max(0, qtyToReturn - initialRejectedQty);

                    if (deductionFromCurrentStock > 0)
                    {
                        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId && p.CompanyId == returnData.CompanyId);
                        if (product != null)
                        {
                            product.CurrentStock -= deductionFromCurrentStock;
                            _context.Products.Update(product);

                            var returnTx = new InventoryTransaction(
                                item.ProductId,
                                -item.ReturnQty,
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
                }

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
                catch { }

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
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var query = _context.PurchaseReturns.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsQuick == isQuick)
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
            query = query.OrderByDescending(x => EF.Property<object>(x, effectiveSortField));
        else
            query = query.OrderBy(x => EF.Property<object>(x, effectiveSortField));

        var pagedData = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync();

        if (pagedData == null || !pagedData.Any())
            return new PurchaseReturnPagedResponse { Items = new List<PurchaseReturnListDto>(), TotalCount = totalCount };

        var supplierIds = pagedData.Select(x => x.SupplierId).Distinct().ToList();
        var supplierNames = await GetSupplierNamesFromMicroservice(supplierIds);

        var pagedIds = pagedData.Select(x => x.Id).ToList();
        
        var grnDetailsList = await _context.PurchaseReturnItems
            .AsNoTracking()
            .Where(ri => pagedIds.Contains(ri.PurchaseReturnId) && ri.CompanyId == companyId)
            .Select(ri => new { ri.PurchaseReturnId, ri.GrnRef })
            .Distinct()
            .ToListAsync();

        var grnLookup = grnDetailsList
            .GroupBy(x => x.PurchaseReturnId)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(i => i.GrnRef).Distinct()));

        var items = pagedData.Select(x => new PurchaseReturnListDto
        {
            Id = x.Id,
            ReturnNumber = x.ReturnNumber,
            ReturnDate = x.ReturnDate,
            SupplierName = supplierNames.GetValueOrDefault(x.SupplierId, "Unknown"),
            GrnRef = grnLookup.GetValueOrDefault(x.Id, "N/A"),
            TotalAmount = x.GrandTotal,
            Status = "Completed",
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
        var purchaseReturn = await _context.PurchaseReturns
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId);

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
            Status = "Completed",
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
        var data = await _context.PurchaseReturns
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId &&
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
        var returns = await _context.PurchaseReturns
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.Status == "Confirmed" && (x.GatePassNo == null || x.GatePassNo == ""))
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
        var records = await _context.PurchaseReturns
            .Where(x => ids.Contains(x.Id) && x.CompanyId == companyId)
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
        var queryBase = _context.PurchaseReturns.Where(x => x.CompanyId == companyId && x.IsQuick == isQuick);

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
}
