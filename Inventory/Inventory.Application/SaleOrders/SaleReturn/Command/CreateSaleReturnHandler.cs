using Inventory.Domain.Entities;
using Inventory.Application.Common.Interfaces;
using MediatR;
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
        public CreateSaleReturnHandler(ISaleReturnRepository repo) => _repo = repo;

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

            var items = dto.Items.Select(i =>
            {
                var taxableAmount = (i.ReturnQty * i.UnitPrice) - i.DiscountAmount;
                var taxAmount = taxableAmount * (i.TaxPercentage / 100m);
                var totalAmount = taxableAmount + taxAmount;

                Console.WriteLine($"[CreateReturn] Item: {i.ProductId} | Qty: {i.ReturnQty} | Rate: {i.UnitPrice} | Disc: {i.DiscountAmount}");
                Console.WriteLine($"[CreateReturn] Taxable: {taxableAmount} | Tax: {taxAmount} | Total: {totalAmount}");
                
                return new SaleReturnItem
                {
                    CompanyId = dto.CompanyId ?? Guid.Empty,
                    BranchId = dto.BranchId,
                    ProductId = i.ProductId,
                    ReturnQty = i.ReturnQty,
                    UnitPrice = i.UnitPrice,
                    DiscountPercent = i.DiscountPercent,
                    DiscountAmount = i.DiscountAmount,
                    TaxPercentage = i.TaxPercentage,
                    TaxAmount = taxAmount,
                    TotalAmount = totalAmount, // Backend Calculation
                    Reason = i.Reason,
                    ItemCondition = i.ItemCondition,
                    MfgDate = i.MfgDate,
                    ExpDate = i.ExpDate,
                    WarehouseId = i.WarehouseId,
                    RackId = i.RackId,
                    CreatedOn = DateTime.Now,
                    CreatedBy = i.CreatedBy ?? dto.CreatedBy,
                    ModifiedBy = i.ModifiedBy ?? dto.ModifiedBy,
                    ModifiedOn = DateTime.Now
                };
            }).ToList();

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
                CreatedOn = DateTime.Now,
                CreatedBy = dto.CreatedBy,
                ModifiedBy = dto.ModifiedBy,
                ModifiedOn = DateTime.Now,
                
                // Header Level Aggregations
                SubTotal = items.Sum(x => x.ReturnQty * x.UnitPrice),
                DiscountAmount = items.Sum(x => x.DiscountAmount),
                TaxAmount = items.Sum(x => x.TaxAmount),
                TotalAmount = items.Sum(x => x.TotalAmount),

                ReturnItems = items
            };

            return await _repo.CreateSaleReturnAsync(header);
        }
    }
}
