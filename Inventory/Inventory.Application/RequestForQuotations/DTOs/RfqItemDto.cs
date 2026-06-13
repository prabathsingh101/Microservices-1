using System;
using Inventory.Domain.Entities;

public class RfqItemDto
{
    public Guid Id { get; set; }
    public Guid RfqId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? Discount { get; set; }
    public decimal? TotalCost { get; set; }

    public static RfqItemDto FromEntity(RequestForQuotationItem entity)
    {
        if (entity == null) return null!;

        return new RfqItemDto
        {
            Id = entity.Id,
            RfqId = entity.RfqId,
            ProductId = entity.ProductId,
            ProductName = entity.Product?.Name ?? "NA",
            Unit = entity.Product?.Unit ?? "PCS",
            Qty = entity.Qty,
            UnitPrice = entity.UnitPrice,
            TaxRate = entity.TaxRate,
            Discount = entity.Discount,
            TotalCost = entity.TotalCost
        };
    }
}
