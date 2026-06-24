using Inventory.Application.Common.Interfaces;
using MediatR;

namespace Inventory.Application.PurchaseOrders.Commands.Update
{
    public class UpdatePurchaseOrderHandler : IRequestHandler<UpdatePurchaseOrderCommand, bool>
    {
        private readonly IPurchaseOrderRepository _repo;
        private readonly IUnitOfWork _uow;

        public UpdatePurchaseOrderHandler(IPurchaseOrderRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public async Task<bool> Handle(UpdatePurchaseOrderCommand request, CancellationToken ct)
        {
            var dto = request.Dto;

            // 1. Fetch existing PO with child items using Repository
            var po = await _repo.GetByIdWithItemsAsync(dto.Id, ct);

            if (po == null) return false;

            // 2. Update Header Fields
            po.SupplierId = dto.SupplierId;
            po.SupplierName = dto.SupplierName;

            // Safety check for nested PriceList object
            po.PriceListId = dto.PriceList != null ? dto.PriceList.Id : dto.PriceListId;

            po.PoDate = dto.PoDate;
            po.ExpectedDeliveryDate = dto.ExpectedDeliveryDate;
            po.Remarks = dto.Remarks;
            po.PoNumber = dto.PoNumber;
            po.TotalQuantity = dto.TotalQuantity;
            po.TotalTax = dto.TotalTax;
            po.GrandTotal = dto.GrandTotal;
            po.SubTotal = dto.SubTotal;
            po.TaxType = dto.TaxType;
            po.TdsPercent = dto.TdsPercent;
            po.TdsAmount = dto.TdsAmount;
            po.TcsPercent = dto.TcsPercent;
            po.TcsAmount = dto.TcsAmount;
            po.IgstAmount = dto.IgstAmount;
            po.CgstAmount = dto.CgstAmount;
            po.SgstAmount = dto.SgstAmount;
            
            // Map RCM Properties
            po.IsRcm = dto.IsRcm;
            po.RcmGstRate = dto.RcmGstRate;
            po.RcmTaxAmount = dto.RcmTaxAmount;
            po.RcmCgstAmount = dto.RcmCgstAmount;
            po.RcmSgstAmount = dto.RcmSgstAmount;
            po.RcmIgstAmount = dto.RcmIgstAmount;
            po.RcmPaid = dto.RcmPaid;
            po.RcmPaidDate = dto.RcmPaidDate;

            // Audit tracking: Update updated fields, leave CreatedBy unchanged
            po.ModifiedOn = DateTime.UtcNow;
            po.ModifiedBy = dto.ModifiedBy;

            // 3. Syncing Logic: Identify items that were removed in UI
            if (dto.Items != null)
            {
                var itemsToRemove = po.Items
                    .Where(existing => !dto.Items.Any(d => d.Id == existing.Id))
                    .ToList();

                foreach (var item in itemsToRemove)
                {
                    _repo.RemoveItem(item);
                }

                // 4. Update existing items or Add new ones
                foreach (var itemDto in dto.Items)
                {
                    // Existing item check (assuming Id != Guid.Empty for existing items)
                    var existingItem = po.Items.FirstOrDefault(i => i.Id == itemDto.Id && i.Id != Guid.Empty);

                    if (existingItem != null)
                    {
                        // Update existing record fields
                        existingItem.ProductId = itemDto.ProductId;
                        existingItem.Qty = itemDto.Qty;
                        existingItem.Unit = itemDto.Unit ?? existingItem.Unit;
                        existingItem.Rate = itemDto.Rate;
                        existingItem.DiscountPercent = itemDto.DiscountPercent;
                        existingItem.GstPercent = itemDto.GstPercent;
                        existingItem.TaxAmount = itemDto.TaxAmount;
                        existingItem.Total = itemDto.Total;
                        existingItem.MfgDate = itemDto.ManufacturingDate;
                        existingItem.ExpDate = itemDto.ExpiryDate;
                    }
                    else
                    {
                        // Add new item to the collection (EF will handle ID generation)
                        po.Items.Add(new PurchaseOrderItem
                        {
                            PurchaseOrderId = po.Id, // Foreign Key link
                            ProductId = itemDto.ProductId,
                            Qty = itemDto.Qty,
                            Unit = itemDto.Unit ?? "Nos",
                            Rate = itemDto.Rate,
                            DiscountPercent = itemDto.DiscountPercent,
                            GstPercent = itemDto.GstPercent,
                            TaxAmount = itemDto.TaxAmount,
                            Total = itemDto.Total,
                            MfgDate = itemDto.ManufacturingDate,
                            ExpDate = itemDto.ExpiryDate
                        });
                    }
                }
            }

            // 5. Inform Repository and Commit using Unit of Work
            _repo.Update(po);

            // 
            // Returns true if database reflects one or more changes
            return await _uow.SaveChangesAsync(ct) > 0;
        }
    }
}
