using System;
using System.Collections.Generic;

namespace Inventory.Application.PurchaseReturn.DTOs;

public class PoPendingRefundDto
{
    public Guid PurchaseReturnId { get; set; }
    public string ReturnNumber { get; set; }
    public DateTime ReturnDate { get; set; }
    public string Remarks { get; set; }
    public List<PoPendingRefundItemDto> Items { get; set; } = new();
}

public class PoPendingRefundItemDto
{
    public Guid PurchaseReturnId { get; set; }
    public string PurchaseReturnNumber { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal ReturnQty { get; set; }
    public decimal Rate { get; set; }
    public decimal GstPercent { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TotalAmount { get; set; }
}
