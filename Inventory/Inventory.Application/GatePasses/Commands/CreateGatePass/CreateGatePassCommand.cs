using Inventory.Application.GatePasses.DTOs;
using MediatR;
using System;

namespace Inventory.Application.GatePasses.Commands.CreateGatePass
{
    public class CreateGatePassCommand : IRequest<GatePassDto>
    {
        public string PassType { get; set; } // Inward, Outward
        public int ReferenceType { get; set; }
        public string ReferenceId { get; set; }
        public string ReferenceNo { get; set; }
        public string? InvoiceNo { get; set; }
        public string PartyName { get; set; } // Supplier/Customer
        public string VehicleNo { get; set; }
        public string? VehicleType { get; set; }
        public string DriverName { get; set; }
        public string DriverPhone { get; set; }
        public string? TransporterName { get; set; }
        public decimal TotalQty { get; set; }
        public decimal? TotalWeight { get; set; } // Optional
        public DateTime GateEntryTime { get; set; } // In/Out Time
        public string SecurityGuard { get; set; }
        public int Status { get; set; }
        public string? Remarks { get; set; }
        public string? CreatedBy { get; set; }
        public Guid? CompanyId { get; set; }
        public string? BranchId { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}
