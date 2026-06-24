namespace Inventory.Application.PurchaseOrders.DTOs
{
    public class UpdatePurchaseOrderDto
    {
        public Guid Id { get; set; }
        public Guid SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? PoNumber { get; set; }
        public Guid PriceListId { get; set; }
        public Guid CompanyId { get; set; }
        public string? BranchId { get; set; }

        public PriceListUpdateDto? PriceList { get; set; }

        public DateTime PoDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string? Remarks { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalTax { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal SubTotal { get; set; }
        public string? TaxType { get; set; } // local, interState
        public decimal? TdsPercent { get; set; }
        public decimal? TdsAmount { get; set; }
        public decimal? TcsPercent { get; set; }
        public decimal? TcsAmount { get; set; }
        public decimal? IgstAmount { get; set; }
        public decimal? CgstAmount { get; set; }
        public decimal? SgstAmount { get; set; }
        public bool IsRcm { get; set; }
        public decimal? RcmGstRate { get; set; }
        public decimal? RcmTaxAmount { get; set; }
        public decimal? RcmCgstAmount { get; set; }
        public decimal? RcmSgstAmount { get; set; }
        public decimal? RcmIgstAmount { get; set; }
        public bool RcmPaid { get; set; }
        public DateTime? RcmPaidDate { get; set; }
        public List<UpdatePurchaseOrderItemDto>? Items { get; set; }
    }

    public class PriceListUpdateDto
    {
        public Guid Id { get; set; }
    }
}
