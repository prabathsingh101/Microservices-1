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
        private readonly InventoryDbContext _context;

        private readonly INotificationRepository _notificationRepository;

        private readonly ISupplierClient _supplierClient;

        public GRNRepository(InventoryDbContext context,
            INotificationRepository notificationRepository,
            ISupplierClient supplierClient)
        {
            _context = context;
            _notificationRepository = notificationRepository;
            _supplierClient = supplierClient;
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
        //            item.UpdatedOn = DateTime.Now;

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
            if (header.PurchaseOrderId == 0)
            {
                throw new Exception("Purchase Order Reference is missing. Cannot save GRN.");
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1. Fetch PO and Products (Use AsNoTracking to get fresh DB values on retry)
                    var po = await _context.PurchaseOrders
                                           .Include(p => p.Items)
                                           .FirstOrDefaultAsync(p => p.Id == header.PurchaseOrderId);

                    if (po != null)
                    {
                        header.SupplierId = po.SupplierId;
                        header.IsQuick = po.IsQuick; // Sync flag from PO to GRN
                    }

                    var productIds = details.Select(d => d.ProductId).Distinct().ToList();
                    var products = await _context.Products
                                                 .Where(p => productIds.Contains(p.Id))
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
                        // Link detail to header (EF will automatically fill header.Id on save)
                        item.GRNHeaderId = 0; // Will be set by EF navigation 
                        item.CreatedOn = DateTime.Now;
                        item.UpdatedOn = DateTime.Now;
                        
                        // Add detail to header collection
                        header.GRNItems ??= new List<GRNDetail>();
                        header.GRNItems.Add(item);

                        var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                        if (product != null)
                        {
                            // CRITICAL FIX: Only add AcceptedQty or ReceivedQty based on business logic.
                            // Typically, only Accepted items should increase available stock.
                            // If user rejected some items, those shouldn't be in CurrentStock.
                            decimal qtyToIncrease = item.ReceivedQty - item.RejectedQty;
                            
                            product.CurrentStock += qtyToIncrease;
                            product.ModifiedOn = DateTime.Now;
                            product.ModifiedBy = header.CreatedBy;
                            _context.Products.Update(product);

                            // 🆕 Record Inventory Transaction
                            var transactionRecord = new InventoryTransaction(
                                item.ProductId,
                                qtyToIncrease,
                                header.IsQuick ? "QuickGRN" : "GRN", // Correctly record Quick vs Standard
                                header.GRNNumber,
                                item.WarehouseId,
                                item.RackId,
                                item.MfgDate,
                                item.ExpDate
                            );
                            await _context.InventoryTransactions.AddAsync(transactionRecord);

                            // Low Stock Alert check
                            if (product.CurrentStock <= product.MinStock)
                            {
                                bool alreadyNotified = await _context.AppNotifications
                                    .AnyAsync(n => n.Title.Contains(product.Name) && !n.IsRead && n.Type == "Inventory");

                                if (!alreadyNotified)
                                {
                                    await _notificationRepository.AddNotificationAsync(
                                        "Low Stock Alert",
                                        $"Item '{product.Name}' is low. Current: {product.CurrentStock}, Min: {product.MinStock}",
                                        "Inventory",
                                        "/app/inventory/current-stock"
                                    );
                                }
                            }
                        }

                        // Update PO Item tracked received qty
                        if (po != null)
                        {
                            var poItem = po.Items.FirstOrDefault(pi => pi.ProductId == item.ProductId);
                            if (poItem != null)
                            {
                                poItem.ReceivedQty += item.ReceivedQty;
                                _context.PurchaseOrderItems.Update(poItem);
                            }
                        }
                    }

                    // 5. Update PO Status
                    if (po != null && po.Items.All(i => i.ReceivedQty >= i.Qty))
                    {
                        po.Status = "Received";
                        _context.PurchaseOrders.Update(po);
                    }

                    // 6. Update Gate Pass Status
                    if (!string.IsNullOrEmpty(header.GatePassNo))
                    {
                        string cleanGatePassNo = header.GatePassNo.Trim();
                        var gatePass = await _context.GatePasses
                                                     .FirstOrDefaultAsync(g => g.PassNo.Trim() == cleanGatePassNo);
                        if (gatePass != null)
                        {
                            gatePass.Status = 4; // Completed
                            _context.GatePasses.Update(gatePass);
                        }
                    }

                    // 7. Single Atomic Save
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // --- Notifications & Ledger ---
                    try {
                        await _notificationRepository.AddNotificationAsync(
                            "Goods Received",
                            $"Inventory updated. GRN {header.GRNNumber} generated successfully.",
                            "Inventory",
                            "/app/inventory/grn-list"
                        );

                        await _supplierClient.RecordPurchaseAsync(
                            header.SupplierId,
                            header.TotalAmount,
                            header.GRNNumber,
                            $"Goods Received via GRN: {header.GRNNumber}",
                            header.CreatedBy
                        );
                    } catch (Exception ex) { 
                        Console.WriteLine($"[GRNRepository] Background task error: {ex.Message}");
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
            var lastId = await _context.GRNHeaders.OrderByDescending(x => x.Id).Select(x => x.Id).FirstOrDefaultAsync();
            return $"GRN-{DateTime.Now.Year}-{(lastId + 1022 + 1)}";
        }

        public async Task<POForGRNDTO?> GetPODataForGRN(string poIds, int? grnHeaderId = null, string? gatePassNo = null)
        {
            var idList = new List<int>();
            if (!string.IsNullOrEmpty(poIds))
            {
                idList = poIds.Split(',')
                              .Select(s => int.TryParse(s, out int id) ? id : 0)
                              .Where(id => id > 0)
                              .ToList();
            }

            // 1. View Mode Logic: Agar poIds khali hai lekin grnHeaderId hai, toh header table se sahi POId nikaalein
            if (grnHeaderId != null && !idList.Any())
            {
                var poId = await _context.GRNHeaders
                    .Where(x => x.Id == grnHeaderId)
                    .Select(x => x.PurchaseOrderId)
                    .FirstOrDefaultAsync();

                if (poId > 0) idList.Add(poId);
                else return null; 
            }

            if (!idList.Any()) return null;

            // 3. Fetch PO Data with Items
            var pos = await _context.PurchaseOrders
                .Include(h => h.Items)
                .ThenInclude(i => i.Product)
                .Where(h => idList.Contains(h.Id))
                .ToListAsync();

            if (!pos.Any()) return null;

            // If single PO, keep original behavior for DTO fields
            var firstPO = pos.First();
            var allSupplierIds = pos.Select(p => p.SupplierId).Distinct().ToList();
            bool isBulk = pos.Count > 1;
            bool sameSupplier = allSupplierIds.Count == 1;

            // 4. Map DTO
            var dto = new POForGRNDTO
            {
                POHeaderId = isBulk ? 0 : firstPO.Id,
                PONumber = isBulk ? string.Join(", ", pos.Select(p => p.PoNumber)) : (firstPO.PoNumber ?? ""),
                GrnNumber = grnHeaderId != null ?
                            _context.GRNHeaders.Where(x => x.Id == grnHeaderId).Select(x => x.GRNNumber).FirstOrDefault() :
                            "AUTO-GEN",
                SupplierId = sameSupplier ? allSupplierIds.First() : 0,
                SupplierName = sameSupplier ? (pos.First().SupplierName ?? "Unknown") : "Multiple Suppliers",
                Remarks = grnHeaderId != null ?
                          _context.GRNHeaders.Where(x => x.Id == grnHeaderId).Select(x => x.Remarks).FirstOrDefault() : ""
            };

            var items = new List<POItemForGRNDTO>();

            if (grnHeaderId != null)
            {
                // VIEW MODE: Saved GRN details load karein (Assuming View Mode is always for 1 GRN linked to 1 PO)
                int singlePoId = idList.First();
                items = await (from d in _context.GRNDetails
                             join poi in _context.PurchaseOrderItems on new { d.GRNHeader.PurchaseOrderId, d.ProductId } equals new { poi.PurchaseOrderId, poi.ProductId }
                             where d.GRNHeaderId == grnHeaderId
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
                                 ExpDate = d.ExpDate
                             }).ToListAsync();
            }
            else
            {
                // NEW GRN MODE (Single or Bulk)
                // 1. Fetch returns to check replacements
                var returnLookup = await _context.PurchaseReturnItems
                    .Include(ri => ri.PurchaseReturn)
                    .Where(ri => ri.PurchaseReturn.Items.Any(i => _context.GRNDetails.Any(gd => gd.ProductId == ri.ProductId && idList.Contains(gd.GRNHeader.PurchaseOrderId))))
                    .Join(_context.GRNDetails, ri => ri.GrnRef, gd => gd.GRNHeader.GRNNumber, (ri, gd) => new { ri, gd })
                    .Where(x => idList.Contains(x.gd.GRNHeader.PurchaseOrderId))
                    .GroupBy(x => x.ri.ProductId)
                    .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.ri.ReturnQty) })
                    .ToDictionaryAsync(x => x.ProductId, x => x.Qty);

                foreach (var po in pos)
                {
                    foreach (var d in po.Items)
                    {
                        var netInWarehouse = await _context.GRNDetails
                            .Where(gd => gd.ProductId == d.ProductId && gd.GRNHeader.PurchaseOrderId == po.Id)
                            .SumAsync(gd => gd.ReceivedQty - gd.RejectedQty);

                        var pending = d.Qty - netInWarehouse;
                        decimal proposedRecv = 0;

                        if (!string.IsNullOrEmpty(gatePassNo))
                        {
                            if (returnLookup.Any())
                            {
                                proposedRecv = returnLookup.ContainsKey(d.ProductId) ? returnLookup[d.ProductId] : 0;
                            }
                            else
                            {
                                proposedRecv = pending;
                            }
                            if (proposedRecv > pending) proposedRecv = pending;
                        }
                        else
                        {
                            proposedRecv = pending > 0 ? pending : 0;
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
                            IsReplacement = returnLookup.ContainsKey(d.ProductId),
                            PONumber = po.PoNumber,
                            POId = po.Id,
                            SupplierId = po.SupplierId,
                            SupplierName = po.SupplierName,
                            WarehouseId = d.Product?.DefaultWarehouseId,
                            RackId = d.Product?.DefaultRackId,
                            MfgDate = d.MfgDate,
                            ExpDate = d.ExpDate
                        });
                    }
                }
            }

            dto.Items = items;
            return dto;
        }
        



        public async Task<GRNPagedResponseDto> GetGRNPagedListAsync(string search, string sortField, string sortOrder, int pageIndex, int pageSize, bool isQuick = false)
        {
            var query = _context.GRNHeaders.Where(x => x.IsQuick == isQuick).AsQueryable();

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
                ReceivedDate = g.ReceivedDate,
                Status = g.Status,
                GatePassNo = g.GatePassNo,
                TotalAmount = g.TotalAmount,  // GRN Total Amount
                PaymentStatus = "Unpaid",  // Default - To be calculated from Supplier Ledger

                Items = g.GRNItems.Select(d => new GRNItemSummaryDto
                {
                    ProductName = d.Product.Name,
                    OrderedQty = d.OrderedQty,
                    ReceivedQty = d.ReceivedQty,

                    // FIX: Pending calculation for historical view
                    // Hum PO Item ki cumulative 'ReceivedQty' ke bajaye transaction level logic use karenge
                    // Pending = Total Ordered - Jo is GRN tak total receive ho chuka tha
                    // FIX: Pending logic should only apply to actual PO items (OrderedQty > 0)
                    PendingQty = d.OrderedQty > 0 ? (d.OrderedQty - (
                        _context.GRNDetails
                            .Where(prev => prev.ProductId == d.ProductId &&
                                           prev.GRNHeader.PurchaseOrderId == g.PurchaseOrderId &&
                                           prev.GRNHeader.CreatedOn <= g.CreatedOn)
                            .Sum(prev => prev.ReceivedQty - prev.RejectedQty)
                    )) : 0,

                    RejectedQty = d.RejectedQty,
                    ActualRejectedQty = (d.Rack.Name.Contains("E1") || (d.Rack.Description != null && d.Rack.Description.Contains("Expired"))) ? 0 : d.RejectedQty,
                    ExpiredQty = (d.Rack.Name.Contains("E1") || (d.Rack.Description != null && d.Rack.Description.Contains("Expired"))) ? d.RejectedQty : 0,
                    UnitRate = d.UnitRate,
                    RackName = d.Rack.Name,
                    IsExpired = (d.Rack.Name.Contains("E1") || (d.Rack.Description != null && d.Rack.Description.Contains("Expired")))
                }).ToList(),

                TotalRejected = g.GRNItems.Sum(d => d.RejectedQty),
                TotalActualRejected = g.GRNItems.Where(d => !(d.Rack.Name.Contains("E1") || (d.Rack.Description != null && d.Rack.Description.Contains("Expired")))).Sum(d => d.RejectedQty),
                TotalExpired = g.GRNItems.Where(d => (d.Rack.Name.Contains("E1") || (d.Rack.Description != null && d.Rack.Description.Contains("Expired")))).Sum(d => d.RejectedQty)
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
                _ => isDesc ? projectedQuery.OrderByDescending(x => x.Id) : projectedQuery.OrderByDescending(x => x.Id)
            };

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

                    // Fetch Supplier Balances
                    var supplierIds = items.Select(x => x.SupplierId).Distinct().ToList();
                    var supplierBalancesTask = _supplierClient.GetSupplierBalancesAsync(supplierIds);

                    await Task.WhenAll(paidAmountsTask, supplierBalancesTask);

                    var paidAmounts = paidAmountsTask.Result;
                    var supplierBalances = supplierBalancesTask.Result;

                    foreach (var item in items)
                    {
                        decimal totalPaidAmount = 0;
                        
                        // Prioritize GRN-specific payment matches
                        if (paidAmounts != null && paidAmounts.ContainsKey(item.GRNNo))
                        {
                            totalPaidAmount = paidAmounts[item.GRNNo];
                        }
                        // Fallback to PO-specific match only if GRN match is zero
                        else if (paidAmounts != null && !string.IsNullOrEmpty(item.RefPO) && 
                                 paidAmounts.ContainsKey(item.RefPO))
                        {
                            totalPaidAmount = paidAmounts[item.RefPO];
                        }

                        // Fix for Ledger-Based Payment Status
                        decimal currentSupplierBalance = (supplierBalances != null && supplierBalances.ContainsKey(item.SupplierId)) 
                            ? supplierBalances[item.SupplierId] 
                            : 999999; // Default to high positive to avoid accidental Paid unlock

                        // Logic:
                        // 1. If explicit payments cover the amount -> Paid
                        // 2. If Supplier Balance <= 0 -> Everything is Paid (we owe nothing)
                        // 3. Else Partial or Unpaid

                        if (totalPaidAmount >= item.TotalAmount || currentSupplierBalance <= 0)
                        {
                            item.PaymentStatus = "Paid";
                        }
                        else if (totalPaidAmount > 0)
                        {
                            item.PaymentStatus = "Partial";
                        }
                        else 
                        {
                            item.PaymentStatus = "Unpaid";
                        }

                        item.PaidAmount = totalPaidAmount;
                        // item.SupplierBalance = currentSupplierBalance; // If we wanted to show it
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


        public async Task<GrnPrintDto?> GetGrnDetailsByNumberAsync(string grnNumber)
        {
            // Step 1: GRN Header fetch karein aur uske details ko PO items ke saath join karein
            var grnData = await _context.GRNHeaders
                .Where(h => h.GRNNumber == grnNumber)
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
                        .Where(d => d.GRNHeaderId == h.Id)
                        .Join(_context.PurchaseOrderItems,
                              d => new { h.PurchaseOrderId, d.ProductId },
                              poi => new { poi.PurchaseOrderId, poi.ProductId },
                              (d, poi) => new GrnItemPrintDto
                              {
                                  ProductName = d.Product.Name, //
                                  Sku = d.Product.Sku,
                                  Unit = d.Product.Unit,
                                  OrderedQty = d.OrderedQty,
                                  PendingQty = d.PendingQty,
                                  ReceivedQty = d.ReceivedQty,
                                  AcceptedQty = d.AcceptedQty,
                                  RejectedQty = d.RejectedQty,
                                  UnitRate = d.UnitRate,
                                  DiscountPercent = poi.DiscountPercent,
                                  // PO Table se direct data
                                  GstPercentage = poi.GstPercent,
                                  GstAmount = ((d.ReceivedQty * d.UnitRate) * (1 - poi.DiscountPercent / 100)) * (poi.GstPercent / 100),
                                  Total = (d.ReceivedQty * d.UnitRate) * (1 - poi.DiscountPercent / 100)
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
                var suppliers = await _supplierClient.GetSuppliersByIdsAsync(new List<int> { grnData.SupplierId });
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
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    foreach (var poId in request.PurchaseOrderIds)
                    {
                        // 1. PO aur Items fetch karein
                        var poHeader = await _context.PurchaseOrders
                            .Include(p => p.Items)
                            .FirstOrDefaultAsync(p => p.Id == poId && (p.Status == "Approved" || p.Status == "Partially Received"));

                        if (poHeader == null) continue;

                        // 2. Custom function se GRN Number generate karein
                        string newGrnNumber = await GenerateGRNNumber();

                        DateTime utcNow = DateTime.UtcNow;

                        // 3. Naya GRN Header create karein
                        var grnHeader = new GRNHeader
                        {
                            GRNNumber = newGrnNumber,
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
                                ProductId = item.ProductId,
                                OrderedQty = item.Qty,
                                ReceivedQty = qtyToReceiveNow,
                                AcceptedQty = qtyToReceiveNow - rejectedQty,
                                RejectedQty = rejectedQty,
                                UnitRate = item.Rate,
                                WarehouseId = reqItem?.WarehouseId ?? item.Product?.DefaultWarehouseId,
                                RackId = reqItem?.RackId ?? item.Product?.DefaultRackId,
                                CreatedBy = request.CreatedBy,
                                CreatedOn = utcNow
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
                                    reqItem?.ExpDate
                                );
                                await _context.InventoryTransactions.AddAsync(transactionRecord);
                            }

                            grnTotalAmount += (qtyToReceiveNow - rejectedQty) * item.Rate * (1 + (item.GstPercent / 100));

                            // Update ReceivedQty in PO Item
                            item.ReceivedQty = item.ReceivedQty + qtyToReceiveNow;

                            // STOCK UPDATE
                            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
                            if (product != null)
                            {
                                product.CurrentStock += (qtyToReceiveNow - rejectedQty);
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
                                "/app/inventory/grn-list"
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
    }
}
