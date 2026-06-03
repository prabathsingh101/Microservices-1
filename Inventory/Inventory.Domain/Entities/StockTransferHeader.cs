using System;
using System.Collections.Generic;
using Inventory.Domain.Common;

namespace Inventory.Domain.Entities
{
    public class StockTransferHeader : BaseAuditableEntity
    {
        public Guid Id { get; private set; }
        public string TransferNumber { get; private set; } = null!;
        public DateTime TransferDate { get; private set; }
        
        public Guid FromWarehouseId { get; private set; }
        public virtual Warehouse FromWarehouse { get; private set; } = null!;
        
        public Guid ToWarehouseId { get; private set; }
        public virtual Warehouse ToWarehouse { get; private set; } = null!;

        public string? FromBranchId { get; private set; }
        public string? ToBranchId { get; private set; }

        public string Status { get; private set; } = "Dispatched"; // Draft, Dispatched, Completed, Cancelled
        public string? Remarks { get; private set; }
        
        public string? VehicleRegNo { get; private set; }
        public string? TransporterName { get; private set; }
        public string? DriverName { get; private set; }
        public string? EWayBillNo { get; private set; }

        public virtual ICollection<StockTransferDetail> Items { get; internal set; } = new List<StockTransferDetail>();

        private StockTransferHeader() { }

        public void SetTransferNumber(string number)
        {
            TransferNumber = number;
        }

        public void ReceiveTransfer(string? remarks)
        {
            if (Status != "Dispatched")
            {
                throw new Exception($"Stock transfer cannot be received because its current status is '{Status}'.");
            }
            Status = "Completed";
            if (!string.IsNullOrEmpty(remarks))
            {
                Remarks = string.IsNullOrEmpty(Remarks) 
                    ? $"Received Remarks: {remarks}" 
                    : $"{Remarks} | Received Remarks: {remarks}";
            }
        }

        public StockTransferHeader(
            string transferNumber,
            DateTime transferDate,
            Guid fromWarehouseId,
            Guid toWarehouseId,
            string? fromBranchId,
            string? toBranchId,
            Guid companyId,
            string? remarks = null,
            string? vehicleRegNo = null,
            string? transporterName = null,
            string? driverName = null,
            string? eWayBillNo = null)
        {
            Id = Guid.NewGuid();
            TransferNumber = transferNumber;
            TransferDate = transferDate;
            FromWarehouseId = fromWarehouseId;
            ToWarehouseId = toWarehouseId;
            FromBranchId = fromBranchId;
            ToBranchId = toBranchId;
            CompanyId = companyId;
            Remarks = remarks;
            Status = "Dispatched";
            CreatedOn = DateTime.UtcNow;
            VehicleRegNo = vehicleRegNo;
            TransporterName = transporterName;
            DriverName = driverName;
            EWayBillNo = eWayBillNo;
        }
    }
}
