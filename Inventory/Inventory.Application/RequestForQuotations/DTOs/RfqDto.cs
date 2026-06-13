using System;
using System.Collections.Generic;
using System.Linq;
using Inventory.Domain.Entities;

public class RfqDto
{
    public Guid Id { get; set; }
    public string RfqNo { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int Status { get; set; } // Enum int
    public string StatusName { get; set; } = string.Empty; // Enum string name
    public string? Remarks { get; set; }
    public Guid CompanyId { get; set; }
    public string? BranchId { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public bool IsQuick { get; set; }

    public string? ConvertedPoNumber { get; set; }
    public Guid? ConvertedPoId { get; set; }

    public List<RfqItemDto> Items { get; set; } = new();

    public static RfqDto FromEntity(RequestForQuotation entity)
    {
        if (entity == null) return null!;

        return new RfqDto
        {
            Id = entity.Id,
            RfqNo = entity.RfqNo,
            SupplierId = entity.SupplierId,
            SupplierName = entity.SupplierName,
            CreatedDate = entity.CreatedDate,
            ExpiryDate = entity.ExpiryDate,
            Status = (int)entity.Status,
            StatusName = entity.Status.ToString(),
            Remarks = entity.Remarks,
            CompanyId = entity.CompanyId,
            BranchId = entity.BranchId,
            CreatedBy = entity.CreatedBy,
            CreatedOn = entity.CreatedOn,
            ModifiedBy = entity.ModifiedBy,
            ModifiedOn = entity.ModifiedOn,
            IsQuick = entity.IsQuick,
            Items = entity.Items != null 
                ? entity.Items.Select(RfqItemDto.FromEntity).ToList() 
                : new List<RfqItemDto>()
        };
    }
}
