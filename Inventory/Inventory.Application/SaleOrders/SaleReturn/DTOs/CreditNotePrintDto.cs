using Inventory.Application.Clients.DTOs;

public class CreditNotePrintDto
{
    public string ReturnNumber { get; set; } // SR-202602061900
    public DateTime ReturnDate { get; set; }
    public Guid? CustomerId { get; set; }
    public string CustomerName { get; set; } // tttyyty
    public string SONumber { get; set; } // SO-2026-0005
    public decimal SubTotal { get; set; } // 405.00
    public decimal TotalDiscount { get; set; } // New Field
    public decimal TotalTax { get; set; } // 72.90
    public decimal GrandTotal { get; set; } // 477.90
    public List<ReturnItemPrintDto> Items { get; set; }
    public CompanyProfileDto CompanyInfo { get; set; }
}

public class ReturnItemPrintDto
{
    public string ProductName { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal Total { get; set; }
    public DateTime? MfgDate { get; set; }
    public DateTime? ExpDate { get; set; }
    public bool IsExpiryRequired { get; set; }
}
