using Inventory.Application.Clients;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.GRN.DTOs;
using Inventory.Application.GRN.DTOs.Stock;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories
{
    public class GRNRepository : IGRNRepository
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly InventoryDbContext _context;
        private readonly INotificationRepository _notificationRepository;
        private readonly ISupplierClient _supplierClient;

        public GRNRepository(InventoryDbContext context,
            INotificationRepository notificationRepository,
            ISupplierClient supplierClient,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _notificationRepository = notificationRepository;
            _supplierClient = supplierClient;
            _currentUserService = currentUserService;
        }

        //public async Task<string> SaveGRNWithStockUpdate(GRNHeader header, List<GRNDetail> details)
        //{
        //    // 1. PO Reference Check
        //    // Note: Agar aapka ID Guid hai toh 'header.PurchaseOrderId == Guid.Empty' use karein
        //    if (header.PurchaseOrderId == null)
        //    {
        //        throw new Exception("Purchase Order Reference is missing. Cannot save GRN.");
        //    }

        //    using var transaction = await _context.Database.BeginTransactionAsync();
        //    try
        //    {
        //        // --- FIX: Fetch SupplierId from Purchase Order to avoid '0' in DB --- [cite: 2026-02-04]
        //        var po = await _context.PurchaseOrders
        //                               .FirstOrDefaultAsync(p => p.Id == header.PurchaseOrderId);

        //        if (po != null)
        //        {
        //            header.SupplierId = po.SupplierId; // PO se asali SupplierId utha liya [cite: 2026-02-04]
        //        }

        //        // 2. Header Setup - Existing Logic [cite: 2026-02-04]
        //        header.Status = "Received";
        //        header.CreatedOn = DateTime.Now;
        //        header.CreatedBy = header.CreatedBy;
        //        header.ModifiedBy = header.ModifiedBy;

        //        if (string.IsNullOrEmpty(header.GRNNumber) || header.GRNNumber == "AUTO-GEN")
        //        {
        //            header.GRNNumber = await GenerateGRNNumber();
        //        }

        //        await _context.GRNHeaders.AddAsync(header);
        //        await _context.SaveChangesAsync();

        //        // 3. Batch Fetch Products (Optimization) - Existing Logic [cite: 2026-02-04]
        //        var productIds = details.Select(d => d.ProductId).ToList();
        //        var products = await _context.Products
        //                                     .Where(p => productIds.Contains(p.Id))
        //                                     .ToListAsync();

        //        // 4. Detail Mapping & Stock Update - Existing Logic [cite: 2026-02-04]
        //        foreach (var item in details)
        //        {
        //            item.GRNHeaderId = header.Id;
        //            item.CreatedOn = DateTime.Now;
        //            item.ModifiedOn = DateTime.Now;

        //            await _context.GRNDetails.AddAsync(item);

        //            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
        //            if (product != null)
        //            {
        //                product.CurrentStock += item.ReceivedQty;
        //                product.CreatedOn = DateTime.Now;
        //                product.CreatedBy = header.CreatedBy;
        //                _context.Products.Update(product);
        //            }
        //        }

        //        await _context.SaveChangesAsync();
        //        await transaction.CommitAsync();

        //        // --- NOTIFICATION TRIGGER START ---
        //        // Goods receive hone par "Goods Received" ka alert bhejein
        //        await _notificationRepository.AddNotificationAsync(
        //            "Goods Received",
        //            $"Inventory updated for PO #{header.PurchaseOrderId}. GRN {header.GRNNumber} generated successfully.",
        //            "Inventory",
        //            "/app/inventory/grn-list"
        //        );
        //        // --- NOTIFICATION TRIGGER END ---

        //        return header.GRNNumber;
        //    }
        //    catch (Exception ex)
        //    {
        //        await transaction.RollbackAsync();
        //        throw new Exception($"Error: {ex.Message}");
        //    }
        //}


        public async Task<string> SaveGRNWithStockUpdate(GRNHeader header, List<GRNDetail> details)
        {
            if (header.PurchaseOrderId == Guid.Empty)
            {
                throw new Exception("Purchase Order Reference is missing. Cannot save GRN.");
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var companyId = _currentUserService.CompanyId ?? Guid.Empty;
                    var branchId = _currentUserService.BranchId;
                    // 1. Fetch PO and Products (Use AsNoTracking to get fresh DB values on retry)
                    var po = await _context.PurchaseOrders
                                           .Where(p => p.Id == header.PurchaseOrderId && p.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || p.BranchId == branchId))
                                           .Include(p => p.Items)
                                           .FirstOrDefaultAsync();

                    if (po != null)
                    {
                        header.SupplierId = po.SupplierId;
                        header.IsQuick = po.IsQuick; // Sync flag from PO to GRN
                        if (string.IsNullOrEmpty(header.BranchId))
                        {
                            header.BranchId = po.BranchId;
                        }
                    }

                    var productIds = details.Select(d => d.ProductId).Distinct().ToList();
                    var products = await _context.Products
                                                 .Where(p => productIds.Contains(p.Id) && p.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || p.BranchId == branchId))
                                                 .ToListAsync();

                    DateTime utcNow = DateTime.UtcNow;

                    // 2. Setup Header
                    header.Status = "Received";
                    header.ReceivedDate = header.ReceivedDate != default ? header.ReceivedDate.Date.Add(utcNow.TimeOfDay) : utcNow;
                    if (string.IsNullOrEmpty(header.GRNNumber) || header.GRNNumber == "AUTO-GEN")
                    {
                        header.GRNNumber = await GenerateGRNNumber();
                    }

                    // 3. Update Status and Audit Fields
                    header.CreatedOn = utcNow;

                    // Add Header to context (EF will track it and its items)
                    await _context.GRNHeaders.AddAsync(header);

                    // 4. Process Details and Update Stock
                    foreach (var item in details)
                    {
                        item.CreatedOn = DateTime.Now;
                        item.ModifiedOn = DateTime.Now;
                        
                        header.GRNItems ??= new List<GRNDetail>();
                        
                        // 🆕 Set Batch and Reference on the Detail entity itself
                        item.BatchNumber = string.IsNullOrWhiteSpace(item.BatchNumber) ? header.GRNNumber : item.BatchNumber;
                        item.ReferenceNumber = po?.PoNumber;
                        
                        header.GRNItems.Add(item);

                        // ⚡ REDUNDANT: Products.CurrentStock is no longer used for displays.
                        decimal qtyToIncrease = item.ReceivedQty - item.RejectedQty;

                        // 🚀 UPDATE WAREHOUSE SPECIFIC STOCK
                        if (item.WarehouseId.HasValue && item.WarehouseId != Guid.Empty)
                        {
                            var whStock = await _context.WarehouseStocks
                                .FirstOrDefaultAsync(ws => ws.ProductId == item.ProductId && ws.WarehouseId == item.WarehouseId);
                            
                            if (whStock != null)
                            {
                                whStock.Quantity += qtyToIncrease;
                            }
                            else
                            {
                                await _context.WarehouseStocks.AddAsync(new WarehouseStock
                                {
                                    ProductId = item.ProductId,
                                    WarehouseId = item.WarehouseId.Value,
                                    Quantity = qtyToIncrease,
                                    MinStock = 0, // Default
                                    CompanyId = header.CompanyId,
                                    BranchId = header.BranchId
                                });
                            }
                        }

                        // 🆕 Record Inventory Transaction
                        var transactionRecord = new InventoryTransaction(
                            item.ProductId,
                            qtyToIncrease,
                            header.IsQuick ? "QuickGRN" : "GRN",
                            header.GRNNumber,
                            item.WarehouseId,
                            item.RackId,
                            item.MfgDate,
                            item.ExpDate,
                            header.CompanyId,
                            header.BranchId,
                            po?.PoNumber, // ReferenceNumber (Original PO)
                            string.IsNullOrWhiteSpace(item.BatchNumber) ? header.GRNNumber : item.BatchNumber // BatchNumber
                        );
                        await _context.InventoryTransactions.AddAsync(transactionRecord);

                        // 🆕 Update PO Item via RAW SQL
                        if (po != null)
                        {
                            // FIX: Use Gross ReceivedQty for PO tracking to correctly reflect pending balance
                            await _context.Database.ExecuteSqlRawAsync(
                                "UPDATE PurchaseOrderItems SET ReceivedQty = ReceivedQty + {0} WHERE PurchaseOrderId = {1} AND ProductId = {2} AND CompanyId = {3}",
                                item.ReceivedQty, header.PurchaseOrderId, item.ProductId, header.CompanyId);

                            // 🎯 SETTLE ORIGINAL REJECTION: If this is a replacement, find and settle the rejection [cite: 2026-05-04]
                            if (item.IsReplacement)
                            {
                                var originalRejection = await _context.GRNDetails
                                    .Include(gd => gd.GRNHeader)
                                    .Where(gd => gd.GRNHeader.PurchaseOrderId == header.PurchaseOrderId 
                                                 && gd.ProductId == item.ProductId 
                                                 && gd.RejectedQty > 0 
                                                 && !gd.IsSettled
                                                 && gd.CompanyId == header.CompanyId)
                                    .OrderBy(gd => gd.CreatedOn)
                                    .FirstOrDefaultAsync();

                                if (originalRejection != null)
                                {
                                    originalRejection.IsSettled = true;
                                    _context.GRNDetails.Update(originalRejection);
                                }
                            }
                        }
                    }

                    // 5. Update PO Status via RAW SQL
                    // FIX: Use Net Accepted Qty (ReceivedQty - RejectedQty) to determine if truly fully received
                    if (po != null)
                    {
                        // First check if there are any rejections
                        var hasRejections = details.Any(d => d.RejectedQty > 0);
                        
                        if (hasRejections)
                        {
                            // With rejections: mark as Partially Received + reset isDispatched
                            // Supplier must dispatch again for replacements (strict workflow)
                            await _context.Database.ExecuteSqlRawAsync(
                                @"UPDATE PurchaseOrders SET Status = 'Partially Received', IsDispatched = 0
                                  WHERE Id = {0} AND CompanyId = {1}",
                                header.PurchaseOrderId, header.CompanyId);
                        }
                        else
                        {
                            // No rejections: update to Received only if all items fully received
                            await _context.Database.ExecuteSqlRawAsync(
                                @"UPDATE PurchaseOrders SET Status = 'Received', CompanyId = COALESCE(CompanyId, {0}) 
                                  WHERE Id = {1} AND CompanyId = {0} AND NOT EXISTS (SELECT 1 FROM PurchaseOrderItems WHERE PurchaseOrderId = {1} AND ReceivedQty < Qty AND CompanyId = {0})",
                                header.CompanyId, header.PurchaseOrderId);
                        }
                    }

                    // 6. Update Gate Pass Status
                    if (!string.IsNullOrEmpty(header.GatePassNo))
                    {
                        await _context.Database.ExecuteSqlRawAsync(
                            "UPDATE GatePasses SET Status = 4, CompanyId = COALESCE(CompanyId, {0}) WHERE PassNo = {1} AND CompanyId = {0}",
                            header.CompanyId, header.GatePassNo.Trim());
                    }

                    // 7. Save remaining tracked entities (GRNHeader, InventoryTransactions)
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Ledger trigger removed from Repository to avoid double entry.
                    // It is now handled centrally in the CreateGRNHandler.
                    try {
                        await _notificationRepository.AddNotificationAsync(
                            "Goods Received",
                            $"Inventory updated. GRN {header.GRNNumber} generated successfully.",
                            "Inventory",
                            "/app/inventory/grn-list",
                            header.BranchId,
                            header.CompanyId
                        );
                    } catch (Exception ex) { 
                        Console.WriteLine($"[GRNRepository] Notification error: {ex.Message}");
                    }

                    return header.GRNNumber;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<string> GenerateGRNNumber()
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;
            var count = await _context.GRNHeaders.Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId)).CountAsync();
            return $"GRN-{DateTime.Now.Year}-{(count + 1022 + 1)}";
        }

        public async Task<POForGRNDTO?> GetPODataForGRN(string poIds, Guid? grnHeaderId = null, string? gatePassNo = null)
        {
            var idList = new List<Guid>();
            if (!string.IsNullOrEmpty(poIds))
            {
                idList = poIds.Split(',')
                              .Select(s => Guid.TryParse(s, out Guid id) ? id : Guid.Empty)
                              .Where(id => id != Guid.Empty)
                              .ToList();
            }

            // 1. View Mode Logic: Agar poIds khali hai lekin grnHeaderId hai, toh header table se sahi POId nikaalein
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;
            if (grnHeaderId != null && !idList.Any())
            {
                var poId = await _context.GRNHeaders
                    .Where(x => x.Id == grnHeaderId && x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
                    .Select(x => x.PurchaseOrderId)
                    .FirstOrDefaultAsync();

                if (poId != Guid.Empty) idList.Add(poId);
                else return null; 
            }

            if (!idList.Any()) return null;

            // 3. Fetch PO Data with Items
            var pos = await _context.PurchaseOrders
                .Include(h => h.Items)
                .ThenInclude(i => i.Product)
                .Where(h => idList.Contains(h.Id) && h.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || h.BranchId == branchId))
                .ToListAsync();

            if (!pos.Any()) return null;

            // 🎯 AUTOMATIC DISPATCH: Mark loaded POs as Dispatched if creating a new GRN
            if (grnHeaderId == null)
            {
                bool anyUpdated = false;
                foreach (var po in pos)
                {
                    if (!po.IsDispatched)
                    {
                        po.IsDispatched = true;
                        po.ModifiedOn = DateTime.Now;
                        _context.PurchaseOrders.Update(po);
                        anyUpdated = true;
                    }
                }
                if (anyUpdated)
                {
                    await _context.SaveChangesAsync();
                }
            }

            // If single PO, keep original behavior for DTO fields
            var firstPO = pos.First();
            var allSupplierIds = pos.Select(p => p.SupplierId).Distinct().ToList();
            bool isBulk = pos.Count > 1;
            bool sameSupplier = allSupplierIds.Count == 1;

            // 4. Map DTO
            var dto = new POForGRNDTO
            {
                POHeaderId = isBulk ? Guid.Empty : firstPO.Id,
                PONumber = isBulk ? string.Join(", ", pos.Select(p => p.PoNumber)) : (firstPO.PoNumber ?? ""),
                GrnNumber = grnHeaderId != null ?
                            _context.GRNHeaders.Where(x => x.Id == grnHeaderId && x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId)).Select(x => x.GRNNumber).FirstOrDefault() :
                            "AUTO-GEN",
                SupplierId = sameSupplier ? allSupplierIds.First() : Guid.Empty,
                SupplierName = sameSupplier ? (pos.First().SupplierName ?? "Unknown") : "Multiple Suppliers",
                Remarks = grnHeaderId != null ?
                          _context.GRNHeaders.Where(x => x.Id == grnHeaderId && x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId)).Select(x => x.Remarks).FirstOrDefault() : ""
            };

            var items = new List<POItemForGRNDTO>();

            if (grnHeaderId != null)
            {
                // VIEW MODE: Saved GRN details load karein (Assuming View Mode is always for 1 GRN linked to 1 PO)
                Guid singlePoId = idList.First();
                items = await (from d in _context.GRNDetails
                             join poi in _context.PurchaseOrderItems on new { d.GRNHeader.PurchaseOrderId, d.ProductId } equals new { poi.PurchaseOrderId, poi.ProductId }
                             where d.GRNHeaderId == grnHeaderId && d.CompanyId == companyId && poi.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || d.BranchId == branchId)
                             select new POItemForGRNDTO
                             {
                                 ProductId = d.ProductId,
                                 ProductName = d.Product.Name ?? "N/A",
                                 OrderedQty = d.OrderedQty,
                                 ReceivedQty = d.ReceivedQty,
                                 RejectedQty = d.RejectedQty,
                                 AcceptedQty = d.ReceivedQty - d.RejectedQty,
                                 UnitRate = d.UnitRate,
                                 PendingQty = d.OrderedQty - (d.ReceivedQty - d.RejectedQty), 
                                 DiscountPercent = poi.DiscountPercent,
                                 GstPercent = poi.GstPercent,
                                 TaxAmount = (d.ReceivedQty - d.RejectedQty) * d.UnitRate * (poi.GstPercent / 100),
                                 WarehouseId = d.WarehouseId,
                                 RackId = d.RackId,
                                 MfgDate = d.MfgDate,
                                 ExpDate = d.ExpDate,
                                 IsExpiryRequired = d.Product != null ? d.Product.IsExpiryRequired : false
                             }).ToListAsync();
            }
            else
            {
                // NEW GRN MODE (Single or Bulk)
                // 1. Fetch returns to check replacements
                var returnLookup = await _context.PurchaseReturnItems
                    .Include(ri => ri.PurchaseReturn)
                    .Where(ri => ri.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || ri.BranchId == branchId) && ri.PurchaseReturn.Items.Any(i => _context.GRNDetails.Any(gd => gd.ProductId == ri.ProductId && idList.Contains(gd.GRNHeader.PurchaseOrderId) && gd.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || gd.BranchId == branchId))))
                    .Join(_context.GRNDetails.Where(gd => gd.CompanyId == companyId), ri => ri.GrnRef, gd => gd.GRNHeader.GRNNumber, (ri, gd) => new { ri, gd })
                    .Where(x => idList.Contains(x.gd.GRNHeader.PurchaseOrderId))
                    .GroupBy(x => x.ri.ProductId)
                    .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.ri.ReturnQty) })
                    .ToDictionaryAsync(x => x.ProductId, x => x.Qty);

                // 1.5. Fetch unsettled rejections to check replacements (even if no return was recorded)
                var rejectionLookup = await _context.GRNDetails
                    .Where(gd => gd.CompanyId == companyId 
                        && idList.Contains(gd.GRNHeader.PurchaseOrderId) 
                        && gd.RejectedQty > 0 
                        && !gd.IsSettled)
                    .GroupBy(gd => gd.ProductId)
                    .Select(g => new { ProductId = g.Key, Qty = g.Sum(gd => gd.RejectedQty) })
                    .ToDictionaryAsync(x => x.ProductId, x => x.Qty);

                // 🎯 Fetch last GRN warehouse/rack per product (for auto-fill)
                var productIdsInPos = pos.SelectMany(p => p.Items.Select(i => i.ProductId)).Distinct().ToList();
                var lastLocationLookup = new Dictionary<Guid, (Guid? WarehouseId, Guid? RackId)>();
                
                foreach (var prodId in productIdsInPos)
                {
                    var lastGrn = await _context.GRNDetails
                        .Where(gd => gd.CompanyId == companyId && gd.ProductId == prodId && gd.WarehouseId != null && gd.WarehouseId != Guid.Empty && gd.RackId != null && gd.RackId != Guid.Empty)
                        .OrderByDescending(gd => gd.CreatedOn)
                        .Select(gd => new { gd.WarehouseId, gd.RackId })
                        .FirstOrDefaultAsync();
                        
                    if (lastGrn != null)
                    {
                        lastLocationLookup[prodId] = (lastGrn.WarehouseId, lastGrn.RackId);
                    }
                }

                foreach (var po in pos)
                {
                    foreach (var d in po.Items)
                    {
                        var returnedQty = returnLookup.ContainsKey(d.ProductId) ? returnLookup[d.ProductId] : 0;
                        var rejectedQty = rejectionLookup.ContainsKey(d.ProductId) ? rejectionLookup[d.ProductId] : 0;
                        
                        // Calculate total accepted quantity so far across all GRNs for this PO item
                        var grnSummary = _context.GRNDetails
                            .Where(gd => gd.ProductId == d.ProductId && gd.GRNHeader.PurchaseOrderId == po.Id)
                            .Select(gd => new { gd.ReceivedQty, gd.RejectedQty })
                            .ToList();

                        var totalAccepted = grnSummary.Sum(s => s.ReceivedQty - s.RejectedQty);
                        if (totalAccepted < 0) totalAccepted = 0;

                        var netAccepted = Math.Max(0, totalAccepted - returnedQty);

                        // Pending = (Ordered - NetAccepted)
                        var pending = Math.Max(0, d.Qty - netAccepted);
                        decimal proposedRecv;

                        // 🎯 FIX: Prioritize replacement quantity (return items or rejections) even without a gate pass
                        if (returnLookup.Any() || rejectionLookup.Any())
                        {
                            // If this product has a pending replacement, use that quantity. 
                            // If it doesn't, but OTHER items in this PO have replacements, default to 0 
                            // because this is likely a replacement-only delivery.
                            proposedRecv = returnedQty > 0 ? returnedQty : rejectedQty;
                        }
                        else
                        {
                            // Standard flow: Use full pending quantity
                            proposedRecv = pending > 0 ? pending : 0;
                        }

                        // 🛡️ Final safeguard: Never propose more than technically pending
                        if (proposedRecv > pending) proposedRecv = pending;
                        if (proposedRecv < 0) proposedRecv = 0;

                        Guid? lastWhId = null;
                        Guid? lastRackId = null;
                        if (lastLocationLookup.ContainsKey(d.ProductId))
                        {
                            lastWhId = lastLocationLookup[d.ProductId].WarehouseId;
                            lastRackId = lastLocationLookup[d.ProductId].RackId;
                        }

                        // Fetch original rejection dates if it is a replacement
                        DateTime? origMfg = null;
                        DateTime? origExp = null;
                        if (rejectionLookup.ContainsKey(d.ProductId))
                        {
                            var origRej = await _context.GRNDetails
                                .Where(gd => gd.CompanyId == companyId 
                                    && idList.Contains(gd.GRNHeader.PurchaseOrderId) 
                                    && gd.ProductId == d.ProductId
                                    && gd.RejectedQty > 0)
                                .OrderBy(gd => gd.CreatedOn)
                                .Select(gd => new { gd.MfgDate, gd.ExpDate })
                                .FirstOrDefaultAsync();

                            if (origRej != null)
                            {
                                origMfg = origRej.MfgDate;
                                origExp = origRej.ExpDate;
                            }
                        }

                        items.Add(new POItemForGRNDTO
                        {
                            ProductId = d.ProductId,
                            ProductName = d.Product?.Name ?? "N/A",
                            OrderedQty = d.Qty,
                            UnitRate = d.Rate,
                            DiscountPercent = d.DiscountPercent,
                            GstPercent = d.GstPercent,
                            PendingQty = pending,
                            ReceivedQty = proposedRecv,
                            RejectedQty = 0,
                            AcceptedQty = proposedRecv, 
                            TaxAmount = (proposedRecv * d.Rate * (1 - d.DiscountPercent / 100)) * (d.GstPercent / 100),
                            IsReplacement = returnLookup.ContainsKey(d.ProductId) || rejectionLookup.ContainsKey(d.ProductId),
                            PONumber = po.PoNumber,
                            POId = po.Id,
                            SupplierId = po.SupplierId,
                            SupplierName = po.SupplierName,
                            WarehouseId = lastWhId ?? d.Product?.DefaultWarehouseId,
                            RackId = lastRackId ?? d.Product?.DefaultRackId,
                            MfgDate = origMfg ?? d.MfgDate,
                            ExpDate = origExp ?? d.ExpDate,
                            IsExpiryRequired = d.Product != null ? d.Product.IsExpiryRequired : false
                        });
                    }
                }
            }

            dto.Items = items;
            return dto;
        }
        



        public async Task<GRNPagedResponseDto> GetGRNPagedListAsync(string search, string sortField, string sortOrder, int pageIndex, int pageSize, bool isQuick = false)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;
            var query = _context.GRNHeaders.Where(x => x.IsQuick == isQuick && x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId)).AsQueryable();

            // 1. Searching Logic (Existing preserved)
            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(x =>
                    (x.GRNNumber != null && x.GRNNumber.ToLower().Contains(s)) ||
                    (x.PurchaseOrder.PoNumber != null && x.PurchaseOrder.PoNumber.ToLower().Contains(s)) ||
                    (x.PurchaseOrder.SupplierName != null && x.PurchaseOrder.SupplierName.ToLower().Contains(s)));
            }

            // 2. Projection to DTO (Corrected Pending Logic)
            var projectedQuery = query.Select(g => new GRNListDto
            {
                Id = g.Id,
                GRNNo = g.GRNNumber,
                RefPO = g.PurchaseOrder.PoNumber,
                SupplierName = g.PurchaseOrder.SupplierName,
                SupplierId = g.SupplierId,  // For payment navigation
                PurchaseOrderId = g.PurchaseOrderId,
                ReceivedDate = g.ReceivedDate,
                Status = g.Status,
                GatePassNo = g.GatePassNo,
                TotalAmount = g.TotalAmount,  // GRN Total Amount
                PaymentStatus = "Unpaid",  // Default - To be calculated from Supplier Ledger

                Items = g.GRNItems.Select(d => new GRNItemSummaryDto
                {
                    ProductName = d.Product.Name,
                    OrderedQty = d.OrderedQty,
                    ReceivedQty = d.ReceivedQty - (_context.PurchaseReturnItems.Where(ri => ri.GrnRef.Trim().ToLower() == g.GRNNumber.Trim().ToLower() && ri.ProductId == d.ProductId && ri.CompanyId == companyId).Sum(ri => (decimal?)ri.ReturnQty) ?? 0),
                    AcceptedQty = d.AcceptedQty - (_context.PurchaseReturnItems.Where(ri => ri.GrnRef.Trim().ToLower() == g.GRNNumber.Trim().ToLower() && ri.ProductId == d.ProductId && ri.CompanyId == companyId).Sum(ri => (decimal?)ri.ReturnQty) ?? 0),

                    // FIX: Pending calculation for historical view
                    // Hum PO Item ki cumulative 'ReceivedQty' ke bajaye transaction level logic use karenge
                    // Pending = Total Ordered - Jo is GRN tak total 'Net Accepted' ho chuka tha
                    // Net Accepted = Total Received - Total Rejected
                    PendingQty = d.OrderedQty > 0 ? Math.Max(0, d.OrderedQty - (
                        (_context.GRNDetails
                            .Where(prev => prev.ProductId == d.ProductId &&
                                           prev.GRNHeader.PurchaseOrderId == g.PurchaseOrderId &&
                                           prev.GRNHeader.CreatedOn <= g.CreatedOn &&
                                           prev.CompanyId == companyId)
                            .Sum(prev => (decimal?)prev.ReceivedQty - (decimal?)prev.RejectedQty) ?? 0)
                        -
                        (_context.PurchaseReturnItems
                            .Where(ri => ri.ProductId == d.ProductId &&
                                         _context.GRNHeaders.Any(gh => gh.GRNNumber == ri.GrnRef &&
                                                                       gh.PurchaseOrderId == g.PurchaseOrderId &&
                                                                       gh.CreatedOn <= g.CreatedOn) &&
                                         ri.CompanyId == companyId)
                            .Sum(ri => (decimal?)ri.ReturnQty) ?? 0)
                    )) : 0,

                    RejectedQty = d.RejectedQty - (_context.PurchaseReturnItems.Where(ri => ri.GrnRef.Trim().ToLower() == g.GRNNumber.Trim().ToLower() && ri.ProductId == d.ProductId && ri.CompanyId == companyId).Sum(ri => (decimal?)ri.ReturnQty) ?? 0),
                    ReturnedQty = _context.PurchaseReturnItems.Where(ri => ri.GrnRef.Trim().ToLower() == g.GRNNumber.Trim().ToLower() && ri.ProductId == d.ProductId && ri.CompanyId == companyId).Sum(ri => (decimal?)ri.ReturnQty) ?? 0,
                    ActualRejectedQty = (d.Rack.Name.ToLower().Contains("e1") || (d.Rack.Description != null && (d.Rack.Description.ToLower().Contains("expired") || d.Rack.Description.ToLower().Contains("damaged") || d.Rack.Description.ToLower().Contains("rejected")))) ? 0 : d.RejectedQty,
                    ExpiredQty = (d.Rack.Name.ToLower().Contains("e1") || (d.Rack.Description != null && (d.Rack.Description.ToLower().Contains("expired") || d.Rack.Description.ToLower().Contains("damaged") || d.Rack.Description.ToLower().Contains("rejected")))) ? d.RejectedQty : 0,
                    UnitRate = d.UnitRate,
                    RackName = d.Rack.Name,
                    IsExpired = (d.Rack.Name.ToLower().Contains("e1") || (d.Rack.Description != null && (d.Rack.Description.ToLower().Contains("expired") || d.Rack.Description.ToLower().Contains("damaged") || d.Rack.Description.ToLower().Contains("rejected"))))
                }).ToList(),

                TotalRejected = g.GRNItems.Sum(d => d.RejectedQty),
                TotalActualRejected = g.GRNItems.Where(d => !(d.Rack.Name.ToLower().Contains("e1") || (d.Rack.Description != null && (d.Rack.Description.ToLower().Contains("expired") || d.Rack.Description.ToLower().Contains("damaged") || d.Rack.Description.ToLower().Contains("rejected"))))).Sum(d => d.RejectedQty),
                TotalExpired = g.GRNItems.Where(d => (d.Rack.Name.ToLower().Contains("e1") || (d.Rack.Description != null && (d.Rack.Description.ToLower().Contains("expired") || d.Rack.Description.ToLower().Contains("damaged") || d.Rack.Description.ToLower().Contains("rejected"))))).Sum(d => d.RejectedQty)
            });

            // 3. Sorting Fix
            bool isDesc = sortOrder?.ToLower() == "desc";
            string field = sortField?.ToLower().Trim();

            projectedQuery = field switch
            {
                "grnno" or "grnnumber" => isDesc ? projectedQuery.OrderByDescending(x => x.GRNNo) : projectedQuery.OrderBy(x => x.GRNNo),
                "refpo" => isDesc ? projectedQuery.OrderByDescending(x => x.RefPO) : projectedQuery.OrderBy(x => x.RefPO),
                "suppliername" => isDesc ? projectedQuery.OrderByDescending(x => x.SupplierName) : projectedQuery.OrderBy(x => x.SupplierName),
                "receiveddate" => isDesc ? projectedQuery.OrderByDescending(x => x.ReceivedDate) : projectedQuery.OrderBy(x => x.ReceivedDate),
                "totalamount" or "amount" => isDesc ? projectedQuery.OrderByDescending(x => x.TotalAmount) : projectedQuery.OrderBy(x => x.TotalAmount),
                "status" => isDesc ? projectedQuery.OrderByDescending(x => x.Status) : projectedQuery.OrderBy(x => x.Status),
                _ => isDesc ? projectedQuery.OrderByDescending(x => x.Id).ThenByDescending(x => x.ReceivedDate) : projectedQuery.OrderBy(x => x.Id).ThenBy(x => x.ReceivedDate)
            };
            
            // 🎯 NEW DEFAULT: If sorting by ID (random Guid) or fallback, always prioritize latest records
            if (string.IsNullOrEmpty(field) || field == "id")
            {
                projectedQuery = projectedQuery.OrderByDescending(x => x.ReceivedDate);
            }

            // 4. Final Execution
            var totalCount = await projectedQuery.CountAsync();
            var items = await projectedQuery
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // --- CROSS MODULE PAYMENT CHECK ---
            if (items.Any())
            {
                try
                {
                    // Pass both GRN Numbers and PO Numbers for matching
                    var searchTerms = items.Select(x => x.GRNNo).ToList();
                    searchTerms.AddRange(items.Where(x => !string.IsNullOrEmpty(x.RefPO)).Select(x => x.RefPO!).Distinct());
                    
                    var paidAmountsTask = _supplierClient.GetGRNPaymentStatusesAsync(searchTerms);

                    // Fetch Supplier Balances & Names
                    var supplierIds = items.Select(x => x.SupplierId).Distinct().ToList();
                    var supplierBalancesTask = _supplierClient.GetSupplierBalancesAsync(supplierIds);
                    var suppliersTask = _supplierClient.GetSuppliersByIdsAsync(supplierIds);

                    await Task.WhenAll(paidAmountsTask, supplierBalancesTask, suppliersTask);

                    var paidAmounts = paidAmountsTask.Result;
                    var supplierBalances = supplierBalancesTask.Result;
                    var suppliers = suppliersTask.Result;
                    var supplierMap = suppliers?.ToDictionary(s => s.Id, s => s.Name ?? "") ?? new Dictionary<Guid, string>();

                    foreach (var item in items)
                    {
                        if (string.IsNullOrEmpty(item.SupplierName) || item.SupplierName == "Unknown" || item.SupplierName == "N/A" || item.SupplierName == "Multiple Suppliers")
                        {
                            if (supplierMap.TryGetValue(item.SupplierId, out var sName) && !string.IsNullOrEmpty(sName))
                            {
                                item.SupplierName = sName;
                            }
                        }

                        decimal totalPaidAmount = 0;
                        
                        // 🔥 HIGH PRIORITY: Exact GRN Number Match
                        if (paidAmounts != null && paidAmounts.ContainsKey(item.GRNNo) && paidAmounts[item.GRNNo] > 0)
                        {
                            totalPaidAmount = paidAmounts[item.GRNNo];
                        }
                        // 🟢 LOW PRIORITY: Fallback to PO if no GRN-specific payment found
                        else if (paidAmounts != null && !string.IsNullOrEmpty(item.RefPO) && 
                                 paidAmounts.ContainsKey(item.RefPO))
                        {
                            totalPaidAmount = paidAmounts[item.RefPO];
                        }

                        // Fix for Ledger-Based Payment Status
                        decimal currentSupplierBalance = (supplierBalances != null && supplierBalances.ContainsKey(item.SupplierId)) 
                            ? supplierBalances[item.SupplierId] 
                            : 999999; // Default to high positive to avoid accidental Paid unlock

                        // 💰 SMART PAYMENT LOGIC:
                        // Calculate the actual amount that SHOULD be paid (Net Total)
                        // Net Total = Original GRN Total - (Value of Items currently in Rejected Rack) - (Value of Items returned to supplier)
                        
                        // We calculate this dynamically based on the current items (which already account for returns)
                        decimal rejectionValue = item.Items.Sum(i => i.ActualRejectedQty * i.UnitRate);
                        decimal returnedValue = item.Items.Sum(i => i.ReturnedQty * i.UnitRate);
                        
                        decimal netPayableAmount = item.TotalAmount - rejectionValue - returnedValue;
                        
                        // Handle potential negative net total if everything is returned/rejected
                        if (netPayableAmount < 0) netPayableAmount = 0;

                        // 🔍 DEBUG LOGGING
                        Console.WriteLine($"[GRN Payment Status] GRN: {item.GRNNo}, Total: {item.TotalAmount}, Paid: {totalPaidAmount}, RejectedVal: {rejectionValue}, ReturnVal: {returnedValue}, NetPayable: {netPayableAmount}");

                        // 💰 SMART PAYMENT LOGIC:
                        // If the supplier's outstanding balance is fully settled (<= 0.01), then all their GRNs are Paid/Settled.
                        if (currentSupplierBalance <= 0.01m)
                        {
                            item.PaymentStatus = "Paid";
                        }
                        // We use a small epsilon (0.10) to handle potential rounding issues.
                        else if (totalPaidAmount >= (netPayableAmount - 0.10m))
                        {
                            item.PaymentStatus = "Paid";
                        }
                        else if (totalPaidAmount > 0)
                        {
                            item.PaymentStatus = "Partial";
                        }
                        // If net payable is 0 (everything returned/rejected), it's effectively Paid/Settled
                        else if (netPayableAmount <= 0.01m)
                        {
                            item.PaymentStatus = "Paid";
                        }
                        else 
                        {
                            item.PaymentStatus = "Unpaid";
                        }

                        item.PaidAmount = totalPaidAmount;
                        // item.SupplierBalance = currentSupplierBalance; // If we wanted to show it

                        // Calculate if all rejected items are settled
                        var grnDetailsWithRejections = await _context.GRNDetails
                            .Where(gd => gd.GRNHeaderId == item.Id && gd.RejectedQty > 0 && gd.CompanyId == companyId)
                            .Select(gd => new { gd.ProductId })
                            .ToListAsync();

                        bool allSettled = true;
                        if (grnDetailsWithRejections.Any())
                        {
                            foreach (var gdRej in grnDetailsWithRejections)
                            {
                                // Check replacement
                                var hasReplacement = await _context.GRNDetails.AnyAsync(gd => 
                                    gd.GRNHeader.PurchaseOrderId == item.PurchaseOrderId 
                                    && gd.ProductId == gdRej.ProductId 
                                    && gd.IsReplacement == true
                                    && gd.CompanyId == companyId);

                                if (hasReplacement) continue;

                                // Check return
                                var hasReturn = await _context.PurchaseReturnItems.AnyAsync(ri => 
                                    ri.GrnRef == item.GRNNo 
                                    && ri.ProductId == gdRej.ProductId 
                                    && ri.CompanyId == companyId);

                                if (hasReturn) continue;

                                allSettled = false;
                                break;
                            }
                        }
                        else
                        {
                            allSettled = false; // No rejections, so not settled
                        }
                        item.IsRejectionSettled = allSettled;
                    }
                }
                catch (Exception ex)
                {
                    // Log error but show default unpaid
                    Console.WriteLine($"Payment Status Sync Error: {ex.Message}");
                }
            }

            return new GRNPagedResponseDto { Items = items, TotalCount = totalCount };
        }


        public async Task<GrnPrintDto?> GetGrnDetailsByNumberAsync(string grnNumber, Guid? companyId = null)
        {
            var activeCompanyId = companyId ?? _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = companyId.HasValue ? null : _currentUserService.BranchId;
            // Step 1: GRN Header fetch karein aur uske details ko PO items ke saath join karein
            var grnData = await _context.GRNHeaders
                .IgnoreQueryFilters()
                .Where(h => h.GRNNumber == grnNumber && h.CompanyId == activeCompanyId && (string.IsNullOrEmpty(branchId) || h.BranchId == branchId))
                .AsNoTracking()
                .Select(h => new GrnPrintDto
                {
                    Id = h.Id,
                    GrnNumber = h.GRNNumber,
                    PurchaseOrderId = h.PurchaseOrderId,
                    PoNumber = h.PurchaseOrder != null ? h.PurchaseOrder.PoNumber : null,
                    SupplierId = h.SupplierId,
                    ReceivedDate = h.ReceivedDate,
                    Status = h.Status, 
                    GatePassNo = h.GatePassNo,
                    Remarks = h.Remarks,
                    TotalAmount = h.TotalAmount,
                    // Items ko optimize tarike se fetch karne ke liye join logic
                    Items = _context.GRNDetails
                        .IgnoreQueryFilters()
                        .Where(d => d.GRNHeaderId == h.Id && d.CompanyId == activeCompanyId)
                        .Select(d => new GrnItemPrintDto
                        {
                            ProductName = d.Product.Name,
                            Sku = d.Product.Sku,
                            Unit = d.Product.Unit,
                            OrderedQty = d.OrderedQty,
                            PendingQty = d.PendingQty,
                            ReceivedQty = d.ReceivedQty - (_context.PurchaseReturnItems.IgnoreQueryFilters().Where(ri => ri.GrnRef == h.GRNNumber && ri.ProductId == d.ProductId && ri.CompanyId == activeCompanyId).Sum(ri => (decimal?)ri.ReturnQty) ?? 0),
                            AcceptedQty = d.AcceptedQty - (_context.PurchaseReturnItems.IgnoreQueryFilters().Where(ri => ri.GrnRef == h.GRNNumber && ri.ProductId == d.ProductId && ri.CompanyId == activeCompanyId).Sum(ri => (decimal?)ri.ReturnQty) ?? 0),
                            RejectedQty = d.RejectedQty,
                            UnitRate = d.UnitRate,
                            DiscountPercent = d.DiscountPercent,
                            GstPercentage = d.GstPercent,
                            GstAmount = d.TaxAmount,
                            Total = d.Total
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            if (grnData == null) return null;

            // Step 2: Footer Calculations (In-Memory calculation for speed)
            grnData.SubTotal = grnData.Items.Sum(i => i.Total);
            grnData.TotalTaxAmount = grnData.Items.Sum(i => i.GstAmount);
            grnData.TotalAmount = grnData.SubTotal + grnData.TotalTaxAmount;

            // Step 3: Supplier Microservice Call
            try
            {
                var suppliers = await _supplierClient.GetSuppliersByIdsAsync(new List<Guid> { grnData.SupplierId });
                grnData.SupplierName = suppliers.FirstOrDefault()?.Name ?? "Supplier Not Found";
            }
            catch
            {
                grnData.SupplierName = "Service Unavailable";
            }

            return grnData;
        }

        public async Task<bool> CreateBulkGrnFromPoAsync(BulkGrnRequestDto request)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                var companyId = _currentUserService.CompanyId ?? Guid.Empty;
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    foreach (var poId in request.PurchaseOrderIds)
                    {
                        // 1. PO aur Items fetch karein
                        var poHeader = await _context.PurchaseOrders
                            .Include(p => p.Items)
                            .FirstOrDefaultAsync(p => p.Id == poId && p.CompanyId == companyId && (p.Status == "Approved" || p.Status == "Partially Received"));

                        if (poHeader == null) continue;

                        // 2. Custom function se GRN Number generate karein
                        string newGrnNumber = await GenerateGRNNumber();

                        DateTime utcNow = DateTime.UtcNow;

                        // 3. Naya GRN Header create karein
                        var grnHeader = new GRNHeader
                        {
                            GRNNumber = newGrnNumber,
                            CompanyId = request.CompanyId ?? Guid.Empty,
                            BranchId = string.IsNullOrEmpty(request.BranchId) ? poHeader.BranchId : request.BranchId,
                            PurchaseOrderId = poId,
                            SupplierId = poHeader.SupplierId,
                            // Date from UI + Current Time from UTC
                            ReceivedDate = request.ReceivedDate != default ? request.ReceivedDate.Date.Add(utcNow.TimeOfDay) : utcNow,
                            TotalAmount = poHeader.GrandTotal,
                            Status = "Received",
                            Remarks = request.Remarks ?? "Bulk Processed from PO",
                            GatePassNo = request.GatePassNo,
                            CreatedBy = request.CreatedBy,
                            CreatedOn = utcNow
                        };

                        _context.GRNHeaders.Add(grnHeader);
                        await _context.SaveChangesAsync();

                        bool isFullPoReceived = true; 
                        decimal grnTotalAmount = 0;

                        // 4. PO Items ko map karein
                        foreach (var item in poHeader.Items)
                        {
                            // REQ CHECK: Kya ye item request mein hai?
                            var reqItem = request.Items.FirstOrDefault(x => x.POId == poId && x.ProductId == item.ProductId);
                            
                            decimal qtyToReceiveNow = 0;
                            decimal rejectedQty = 0;

                            if (reqItem != null)
                            {
                                qtyToReceiveNow = reqItem.ReceivedQty;
                                rejectedQty = reqItem.RejectedQty;
                            }
                            else
                            {
                                // Fallback: Pure pending quantity (if not specifically passed from UI)
                                qtyToReceiveNow = item.Qty - item.ReceivedQty;
                            }

                            if (qtyToReceiveNow <= 0) continue; 

                            var grnDetail = new GRNDetail
                            {
                                GRNHeaderId = grnHeader.Id,
                                CompanyId = request.CompanyId ?? Guid.Empty,
                                ProductId = item.ProductId,
                                OrderedQty = item.Qty,
                                ReceivedQty = qtyToReceiveNow,
                                AcceptedQty = qtyToReceiveNow - rejectedQty,
                                RejectedQty = rejectedQty,
                                UnitRate = item.Rate,
                                WarehouseId = reqItem?.WarehouseId ?? item.Product?.DefaultWarehouseId,
                                RackId = reqItem?.RackId ?? item.Product?.DefaultRackId,
                                CreatedBy = request.CreatedBy,
                                CreatedOn = utcNow,
                                BatchNumber = string.IsNullOrWhiteSpace(reqItem?.BatchNumber) ? newGrnNumber : reqItem.BatchNumber,
                                ReferenceNumber = poHeader.PoNumber
                            };
                            _context.GRNDetails.Add(grnDetail);

                            // 🆕 Record Inventory Transaction for Bulk
                            if (qtyToReceiveNow - rejectedQty > 0)
                            {
                                var transactionRecord = new InventoryTransaction(
                                    item.ProductId,
                                    qtyToReceiveNow - rejectedQty,
                                    "GRN-BULK",
                                    newGrnNumber,
                                    grnDetail.WarehouseId,
                                    grnDetail.RackId,
                                    reqItem?.MfgDate,
                                    reqItem?.ExpDate,
                                    grnHeader.CompanyId,
                                    grnHeader.BranchId,
                                    poHeader.PoNumber,
                                    string.IsNullOrWhiteSpace(reqItem?.BatchNumber) ? newGrnNumber : reqItem.BatchNumber
                                );
                                await _context.InventoryTransactions.AddAsync(transactionRecord);
                            }

                            grnTotalAmount += (qtyToReceiveNow - rejectedQty) * item.Rate * (1 + (item.GstPercent / 100));

                            // Update ReceivedQty in PO Item (Gross Received for balance tracking)
                            item.ReceivedQty = item.ReceivedQty + qtyToReceiveNow;

                            // ⚡ REDUNDANT: Products.CurrentStock removed.


                            // STOCK UPDATE (WAREHOUSE SPECIFIC)
                            var whId = reqItem?.WarehouseId ?? item.Product?.DefaultWarehouseId;
                            if (whId.HasValue)
                            {
                                var qtyToIncrease = qtyToReceiveNow - rejectedQty;
                                var whStock = await _context.WarehouseStocks
                                    .FirstOrDefaultAsync(ws => ws.ProductId == item.ProductId && ws.WarehouseId == whId);
                                
                                if (whStock != null)
                                {
                                    whStock.Quantity += qtyToIncrease;
                                }
                                else
                                {
                                    await _context.WarehouseStocks.AddAsync(new WarehouseStock
                                    {
                                        ProductId = item.ProductId,
                                        WarehouseId = whId.Value,
                                        Quantity = qtyToIncrease,
                                        MinStock = 0,
                                        CompanyId = grnHeader.CompanyId,
                                        BranchId = grnHeader.BranchId
                                    });
                                }
                            }

                            if (item.ReceivedQty < item.Qty)
                            {
                                isFullPoReceived = false;
                            }
                        }

                        // Update Header total
                        grnHeader.TotalAmount = grnTotalAmount;
                        poHeader.Status = isFullPoReceived ? "GRN Processed" : "Partially Received";

                        // 6. NOTIFICATION & LEDGER TRIGGER
                        try
                        {
                            await _notificationRepository.AddNotificationAsync(
                                "Goods Received",
                                $"Inventory updated for PO #{poId}. GRN {newGrnNumber} generated successfully.",
                                "Inventory",
                                "/app/inventory/grn-list",
                                grnHeader.BranchId,
                                grnHeader.CompanyId
                            );

                            await _supplierClient.RecordPurchaseAsync(
                                poHeader.SupplierId,
                                grnHeader.TotalAmount,
                                grnHeader.GRNNumber,
                                $"Bulk Goods Received for PO #{poHeader.PoNumber} via GRN: {grnHeader.GRNNumber}",
                                request.CreatedBy
                            );
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[GRNRepository] Bulk posting error for PO {poId}: {ex.Message}");
                        }
                    }

                    // 6. Update Gate Pass Status (If applicable)
                    if (!string.IsNullOrEmpty(request.GatePassNo))
                    {
                        string cleanGatePassNo = request.GatePassNo.Trim();
                        var gatePass = await _context.GatePasses
                                                     .FirstOrDefaultAsync(g => g.PassNo.Trim() == cleanGatePassNo);
                        if (gatePass != null)
                        {
                            gatePass.Status = 4; // Completed
                            _context.GatePasses.Update(gatePass);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Bulk GRN Error: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<List<GrnRejectionHistoryDto>> GetGrnRejectionHistoryAsync(string grnNumber)
        {
            Console.WriteLine($"[GRNRepository] Fetching rejection history for: {grnNumber}");
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;

            try 
            {
                // 1. Fetch rejections with product names in a single query
                var rejections = await (from gd in _context.GRNDetails
                                        join gh in _context.GRNHeaders on gd.GRNHeaderId equals gh.Id
                                        join p in _context.Products on gd.ProductId equals p.Id
                                        where gh.GRNNumber == grnNumber && gd.RejectedQty > 0 && gd.CompanyId == companyId
                                        select new 
                                        { 
                                            gd.ProductId, 
                                            ProductName = p.Name, 
                                            gd.RejectedQty, 
                                            gd.IsSettled,
                                            gh.PurchaseOrderId,
                                            gd.Id
                                        }).ToListAsync();

                Console.WriteLine($"[GRNRepository] Found {rejections.Count} rejections for {grnNumber}");
                if (!rejections.Any()) return new List<GrnRejectionHistoryDto>();

                var history = new List<GrnRejectionHistoryDto>();

                foreach (var rej in rejections)
                {
                    Console.WriteLine($"[GRNRepository] Processing rejection for Product: {rej.ProductName}");
                    var item = new GrnRejectionHistoryDto
                    {
                        ProductId = rej.ProductId,
                        ProductName = rej.ProductName ?? "Unknown",
                        RejectedQty = rej.RejectedQty,
                        IsSettled = rej.IsSettled,
                        Status = rej.IsSettled ? "Settled" : "Pending"
                    };

                    // 2. Look for replacements
                    Console.WriteLine($"[GRNRepository] Checking replacements for Product: {rej.ProductId} in PO: {rej.PurchaseOrderId}");
                    var replacement = await (from gd in _context.GRNDetails
                                             join gh in _context.GRNHeaders on gd.GRNHeaderId equals gh.Id
                                             where gh.PurchaseOrderId == rej.PurchaseOrderId 
                                                   && gd.ProductId == rej.ProductId 
                                                   && gd.IsReplacement == true
                                                   && gd.CompanyId == companyId
                                             orderby gh.CreatedOn ascending
                                             select new { gh.GRNNumber, gh.Id }).FirstOrDefaultAsync();

                    if (replacement != null)
                    {
                        Console.WriteLine($"[GRNRepository] Found replacement in {replacement.GRNNumber}");
                        item.Resolution = $"Replaced in {replacement.GRNNumber}";
                        item.ResolutionGrn = replacement.GRNNumber;
                        item.ResolutionGrnId = replacement.Id;
                        item.Status = "Settled";
                        item.IsSettled = true;
                    }
                    else
                    {
                        // 3. Look for Returns (Debit Note)
                        Console.WriteLine($"[GRNRepository] Checking returns for Product: {rej.ProductId}");
                        var returnRef = await _context.PurchaseReturnItems
                            .Where(ri => ri.GrnRef == grnNumber && ri.ProductId == rej.ProductId && ri.CompanyId == companyId)
                            .Select(ri => ri.PurchaseReturn.ReturnNumber)
                            .FirstOrDefaultAsync();

                        if (returnRef != null)
                        {
                            Console.WriteLine($"[GRNRepository] Found return in {returnRef}");
                            item.Resolution = $"Returned in {returnRef}";
                            item.Status = "Settled";
                            item.IsSettled = true;
                        }
                        else
                        {
                            Console.WriteLine($"[GRNRepository] No resolution found for Product: {rej.ProductId}");
                            item.Resolution = "Pending / Replacement Awaited";
                            item.Status = "Pending";
                            item.IsSettled = false;
                        }
                    }

                    history.Add(item);
                }

                Console.WriteLine($"[GRNRepository] Returning {history.Count} history items");
                return history;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GRNRepository] ERROR in GetGrnRejectionHistoryAsync: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
        public async Task<bool> CancelGRNWithStockReversal(Guid grnId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var companyId = _currentUserService.CompanyId ?? Guid.Empty;
                    var branchId = _currentUserService.BranchId;

                    var grnHeader = await _context.GRNHeaders
                        .Include(g => g.GRNItems)
                        .FirstOrDefaultAsync(g => g.Id == grnId && g.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || g.BranchId == branchId));

                    if (grnHeader == null)
                        throw new Exception("GRN not found.");
                        
                    if (grnHeader.Status == "Cancelled")
                        throw new Exception("GRN is already cancelled.");

                    grnHeader.Status = "Cancelled";
                    grnHeader.ModifiedOn = DateTime.UtcNow;
                    _context.GRNHeaders.Update(grnHeader);

                    if (grnHeader.GRNItems != null)
                    {
                        foreach (var item in grnHeader.GRNItems)
                        {
                            decimal qtyToDecrease = item.ReceivedQty - item.RejectedQty;

                            if (item.WarehouseId.HasValue && item.WarehouseId != Guid.Empty)
                            {
                                var whStock = await _context.WarehouseStocks
                                    .FirstOrDefaultAsync(ws => ws.ProductId == item.ProductId && ws.WarehouseId == item.WarehouseId);

                                if (whStock != null)
                                {
                                    whStock.Quantity -= qtyToDecrease;
                                    if (whStock.Quantity < 0) whStock.Quantity = 0;
                                }
                            }

                            var transactionRecord = new InventoryTransaction(
                                item.ProductId,
                                -qtyToDecrease,
                                "GRNCancel",
                                grnHeader.GRNNumber,
                                item.WarehouseId,
                                item.RackId,
                                item.MfgDate,
                                item.ExpDate,
                                grnHeader.CompanyId,
                                grnHeader.BranchId,
                                null, 
                                string.IsNullOrWhiteSpace(item.BatchNumber) ? grnHeader.GRNNumber : item.BatchNumber
                            );
                            await _context.InventoryTransactions.AddAsync(transactionRecord);

                            if (grnHeader.PurchaseOrderId != Guid.Empty)
                            {
                                await _context.Database.ExecuteSqlRawAsync(
                                    "UPDATE PurchaseOrderItems SET ReceivedQty = ReceivedQty - {0} WHERE PurchaseOrderId = {1} AND ProductId = {2} AND CompanyId = {3}",
                                    item.ReceivedQty, grnHeader.PurchaseOrderId, item.ProductId, grnHeader.CompanyId);
                            }
                        }
                    }

                    if (grnHeader.PurchaseOrderId != Guid.Empty)
                    {
                        await _context.Database.ExecuteSqlRawAsync(
                            @"UPDATE PurchaseOrders SET Status = 'Cancelled'
                              WHERE Id = {0} AND CompanyId = {1}",
                            grnHeader.PurchaseOrderId, grnHeader.CompanyId);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<GRNHeader> GetGrnBasicDetailsAsync(Guid grnId)
        {
            return await _context.GRNHeaders.FirstOrDefaultAsync(g => g.Id == grnId);
        }
    }
}
