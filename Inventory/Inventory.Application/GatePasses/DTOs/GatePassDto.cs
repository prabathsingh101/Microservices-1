using Inventory.Domain.Entities;
using System;

namespace Inventory.Application.GatePasses.DTOs
{
    public class GatePassDto
    {
        public Guid Id { get; set; }
        public string PassNo { get; set; }
        public string PassType { get; set; }
        public int ReferenceType { get; set; }
        public string ReferenceId { get; set; }
        public string ReferenceNo { get; set; }
        public string? InvoiceNo { get; set; }
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
        public int Status { get; set; }
        public string? Remarks { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }

        public static GatePassDto FromEntity(GatePass entity)
        {
            return new GatePassDto
            {
                Id = entity.Id,
                PassNo = entity.PassNo,
                PassType = entity.PassType,
                ReferenceType = entity.ReferenceType,
                ReferenceId = entity.ReferenceId,
                ReferenceNo = entity.ReferenceNo,
                InvoiceNo = entity.InvoiceNo,
                PartyName = entity.PartyName,
                VehicleNo = entity.VehicleNo,
                VehicleType = entity.VehicleType,
                DriverName = entity.DriverName,
                DriverPhone = entity.DriverPhone,
                TransporterName = entity.TransporterName,
                TotalQty = entity.TotalQty,
                TotalWeight = entity.TotalWeight,
                GateEntryTime = entity.GateEntryTime,
                SecurityGuard = entity.SecurityGuard,
                Status = entity.Status,
                Remarks = entity.Remarks,
                CreatedBy = entity.CreatedBy,
                CreatedOn = entity.CreatedOn
            };
        }
    }
}
