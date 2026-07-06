using Inventory.Domain.Common;

namespace Inventory.Domain.Entities
{
    public class GatePass : BaseAuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string PassNo { get; set; }
        public string PassType { get; set; } // Inward / Outward
        public int ReferenceType { get; set; } // 1=PO, 2=GRN...
        public string ReferenceId { get; set; } // Changed to string to support GUIDs
        public string ReferenceNo { get; set; }
        public string? InvoiceNo { get; set; } // For Inward
        public string PartyName { get; set; }
        public string VehicleNo { get; set; }
        public string? VehicleType { get; set; }
        public string DriverName { get; set; }
        public string DriverPhone { get; set; }
        public string? TransporterName { get; set; }
        public decimal TotalQty { get; set; }
        public decimal? TotalWeight { get; set; }
        public DateTime GateEntryTime { get; set; }
        public string SecurityGuard { get; set; }
        public int Status { get; set; } // 1=Entered...
        public string? Remarks { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}
