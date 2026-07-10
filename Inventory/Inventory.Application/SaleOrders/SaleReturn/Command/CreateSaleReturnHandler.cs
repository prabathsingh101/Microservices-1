using Inventory.Domain.Entities;
using Inventory.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.SaleOrders.SaleReturn.Command
{
    public class CreateSaleReturnHandler : IRequestHandler<CreateSaleReturnCommand, bool>
    {
        private readonly ISaleReturnRepository _repo;
        private readonly IInventoryDbContext _context;

        public CreateSaleReturnHandler(ISaleReturnRepository repo, IInventoryDbContext context)
        {
            _repo = repo;
            _context = context;
        }

        public async Task<bool> Handle(CreateSaleReturnCommand request, CancellationToken ct)
        {
            var dto = request.Dto;

            // --- 1. VALIDATION LOGIC START ---
            // Isse dashboard par -4 aana band ho jayega kyunki bache huye se zyada return block ho jayega
            foreach (var item in dto.Items)
            {
                // Pass Mfg/Exp dates to handle multi-line same-product orders correctly
                var remainingQty = await _repo.GetRemainingReturnableQtyAsync(dto.SaleOrderId, item.ProductId, item.MfgDate, item.ExpDate);

                if (item.ReturnQty > remainingQty)
                {
                    // Agar remaining quantity se zyada return karne ki koshish ki toh exception dega
                    throw new Exception($"Cannot return {item.ReturnQty} units for Product ID {item.ProductId}. Maximum allowed return is {remainingQty}.");
                }
            }
            // --- VALIDATION LOGIC END ---

            // Resolve Color/Size from ProductVariants for return items (same pattern as CreateGRNHandler)
            var allVariantIds = dto.Items
                .Where(i => i.ProductVariantId.HasValue && i.ProductVariantId.Value != Guid.Empty)
                .Select(i => i.ProductVariantId!.Value)
                .Concat(dto.ExchangeItems?
                    .Where(e => e.ProductVariantId.HasValue && e.ProductVariantId.Value != Guid.Empty)
                    .Select(e => e.ProductVariantId!.Value) ?? Enumerable.Empty<Guid>())
                .Distinct()
                .ToList();

            var variantsMap = new Dictionary<Guid, (string? Color, string? Size)>();
            if (allVariantIds.Any())
            {
                var variantsList = await _context.ProductVariants
                    .Where(pv => allVariantIds.Contains(pv.Id))
                    .Select(pv => new { pv.Id, pv.Color, pv.Size })
                    .ToListAsync(ct);
                variantsMap = variantsList.ToDictionary(v => v.Id, v => (v.Color, v.Size));
            }

            var items = dto.Items.Select(i =>
            {
                // Fix: TotalAmount comes correctly from the frontend. We reverse-calculate tax using inclusive formula.
                var totalAmount = i.TotalAmount;
                var taxAmount = totalAmount - (totalAmount * 100m / (100m + i.TaxPercentage));

                // Resolve Color/Size from variant map (prefer DTO value, fallback to variant lookup)
                string? itemColor = i.Color;
                string? itemSize = i.Size;
                if (i.ProductVariantId.HasValue && variantsMap.TryGetValue(i.ProductVariantId.Value, out var vInfo))
                {
                    itemColor ??= vInfo.Color;
                    itemSize ??= vInfo.Size;
                }

                Console.WriteLine($"[CreateReturn] Item: {i.ProductId} | Qty: {i.ReturnQty} | Rate: {i.UnitPrice} | Disc: {i.DiscountAmount}");
                Console.WriteLine($"[CreateReturn] Tax: {taxAmount} | Total: {totalAmount}");
                
                return new SaleReturnItem
                {
                    CompanyId = dto.CompanyId ?? Guid.Empty,
                    BranchId = dto.BranchId,
                    ProductId = i.ProductId,
                    ProductVariantId = i.ProductVariantId,
                    Color = itemColor,
                    Size = itemSize,
                    ReturnQty = i.ReturnQty,
                    UnitPrice = i.UnitPrice,
                    DiscountPercent = i.DiscountPercent,
                    DiscountAmount = i.DiscountAmount,
                    TaxPercentage = i.TaxPercentage,
                    TaxAmount = taxAmount,
                    TotalAmount = totalAmount,
                    Reason = i.Reason,
                    ItemCondition = i.ItemCondition,
                    MfgDate = i.MfgDate,
                    ExpDate = i.ExpDate,
                    WarehouseId = i.WarehouseId,
                    RackId = i.RackId,
                    BatchNumber = i.BatchNumber,
                    ReferenceNumber = i.ReferenceNumber,
                    CreatedOn = DateTime.Now,
                    CreatedBy = i.CreatedBy ?? dto.CreatedBy,
                    ModifiedBy = i.ModifiedBy ?? dto.ModifiedBy,
                    ModifiedOn = DateTime.Now
                };
            }).ToList();

            var exchangeItems = dto.ExchangeItems != null ? dto.ExchangeItems.Select(e =>
            {
                var totalAmount = e.TotalAmount;
                var taxAmount = totalAmount - (totalAmount * 100m / (100m + e.TaxPercentage));

                // Resolve Color/Size for exchange items
                string? exchColor = e.Color;
                string? exchSize = e.Size;
                if (e.ProductVariantId.HasValue && variantsMap.TryGetValue(e.ProductVariantId.Value, out var vInfo))
                {
                    exchColor ??= vInfo.Color;
                    exchSize ??= vInfo.Size;
                }

                return new SaleExchangeItem
                {
                    CompanyId = dto.CompanyId ?? Guid.Empty,
                    BranchId = dto.BranchId,
                    ProductId = e.ProductId,
                    ProductVariantId = e.ProductVariantId,
                    Color = exchColor,
                    Size = exchSize,
                    Qty = e.Qty,
                    UnitPrice = e.UnitPrice,
                    DiscountPercent = e.DiscountPercent,
                    DiscountAmount = e.DiscountAmount,
                    TaxPercentage = e.TaxPercentage,
                    TaxAmount = taxAmount,
                    TotalAmount = totalAmount,
                    MfgDate = e.MfgDate,
                    ExpDate = e.ExpDate,
                    WarehouseId = e.WarehouseId,
                    RackId = e.RackId,
                    BatchNumber = e.BatchNumber,
                    ReferenceNumber = e.ReferenceNumber,
                    CreatedOn = DateTime.Now,
                    CreatedBy = e.CreatedBy ?? dto.CreatedBy,
                    ModifiedBy = e.ModifiedBy ?? dto.ModifiedBy,
                    ModifiedOn = DateTime.Now
                };
            }).ToList() : new List<SaleExchangeItem>();

            var header = new SaleReturnHeader
            {
                CompanyId = dto.CompanyId ?? Guid.Empty,
                BranchId = dto.BranchId,
                CustomerId = dto.CustomerId,
                SaleOrderId = dto.SaleOrderId,
                ReturnDate = dto.ReturnDate,
                Remarks = dto.Remarks,
                ReturnNumber = "SR-" + DateTime.Now.ToString("yyyyMMddHHmm"),
                Status = "Confirmed",
                IsQuick = dto.IsQuick,
                ReturnMode = dto.ReturnMode,
                CreatedOn = DateTime.Now,
                CreatedBy = dto.CreatedBy,
                ModifiedBy = dto.ModifiedBy,
                ModifiedOn = DateTime.Now,
                
                // Header Level Aggregations
                TotalReturnAmount = items.Sum(x => x.TotalAmount),
                TotalExchangeAmount = exchangeItems.Sum(x => x.TotalAmount),

                SubTotal = items.Sum(x => x.TotalAmount - x.TaxAmount) - exchangeItems.Sum(x => x.TotalAmount - x.TaxAmount),
                DiscountAmount = items.Sum(x => x.DiscountAmount) - exchangeItems.Sum(x => x.DiscountAmount),
                TaxAmount = items.Sum(x => x.TaxAmount) - exchangeItems.Sum(x => x.TaxAmount),
                TotalAmount = items.Sum(x => x.TotalAmount) - exchangeItems.Sum(x => x.TotalAmount),

                ReturnItems = items,
                ExchangeItems = exchangeItems
            };

            return await _repo.CreateSaleReturnAsync(header);
        }
    }
}
