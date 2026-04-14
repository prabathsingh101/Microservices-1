namespace Inventory.Application.PurchaseOrders.DTOs
{
    public class UpdateStatusDTO
    {
        public Guid Id { get; set; }
        public string Status { get; set; } // "Submitted", "Approved", "Rejected"
    }
}
