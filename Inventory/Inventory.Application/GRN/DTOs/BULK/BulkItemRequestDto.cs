using System;

namespace Inventory.Application.GRN.DTOs.BULK
{
    public class BulkItemRequestDto
    {
        public Guid POId { get; set; }
        public Guid ProductId { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal RejectedQty { get; set; }
        public decimal UnitRate { get; set; }
        public Guid? WarehouseId { get; set; }
        public Guid? RackId { get; set; }
        public DateTime? MfgDate { get; set; }
        public DateTime? ExpDate { get; set; }
        public string? BatchNumber { get; set; }
    }
}
