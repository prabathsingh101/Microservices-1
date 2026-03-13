namespace Inventory.Application.PurchaseOrders.DTOs
{
    public class UpdatePurchaseOrderDto
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? PoNumber { get; set; }
        public Guid PriceListId { get; set; }

        public PriceListUpdateDto? PriceList { get; set; }

        public DateTime PoDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string? Remarks { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalTax { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal SubTotal { get; set; }
        public List<UpdatePurchaseOrderItemDto>? Items { get; set; }
    }

    public class PriceListUpdateDto
    {
        public Guid Id { get; set; }
    }
}