public class StockRefillDetailsDto
{
    // --- Header Section (Top Row) ---
    public Guid SupplierId { get; set; }               // 'Select Supplier' Dropdown
    public string SupplierName { get; set; }         // Display Name (From PO History)
    public Guid? PriceListId { get; set; }           // 'Applicable Price List' Dropdown
    public string PoNumber { get; set; }             // 'PO Number' Read-only field
    public DateTime PoDate { get; set; }             // 'PO Date' DatePicker
    public DateTime? ExpectedDelivery { get; set; }  // 'Expected Delivery' DatePicker

    // --- Line Item Section (Table Row) ---
    public Guid ProductId { get; set; }              // Product Unique ID (GUID) [cite: 2026-01-22]
    public string ProductName { get; set; }          // 'Search Product' Text field
    public decimal Qty { get; set; } = 1;            // 'Qty' Input field
    public string Unit { get; set; }                 // 'Unit' Read-only field (e.g., COIL)
    public decimal Rate { get; set; }                // 'Rate' Input field
    public decimal DiscountPercent { get; set; }     // 'Disc %' Input field
    public decimal GstPercent { get; set; }          // 'GST %' Input field
    public decimal TaxAmount { get; set; }           // 'Tax Amt' Read-only calculated field
    public decimal RowTotal { get; set; }            // 'Total' Row calculation

    // --- Footer & Summary Section ---
    public string Remarks { get; set; }              // 'Remarks / Payment Terms' Textarea
    public decimal SubTotal { get; set; }            // 'Sub-Total' Summary
    public decimal TotalTax { get; set; }            // 'Total Tax' Summary
    public decimal GrandTotal { get; set; }          // 'GRAND TOTAL' Final amount
}
