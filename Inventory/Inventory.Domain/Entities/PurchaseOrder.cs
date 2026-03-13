using Inventory.Domain.Entities;
using Inventory.Domain.PriceLists;
using System.Net.NetworkInformation;

public class PurchaseOrder
{
    public int Id { get; set; }
    public string PoNumber { get; set; } = string.Empty; // PO/26-27/0001
    public int SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public Guid PriceListId { get; set; }
    public PriceList? PriceList { get; set; }
    public DateTime PoDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string ? Remarks { get; set; }
    public decimal TotalQuantity { get; set; } //
    public decimal TotalTax { get; set; } //
    public decimal SubTotal { get; set; } //
    public decimal GrandTotal { get; set; } //
    public string Status { get; set; } = "Draft";
    public string? CreatedBy { get; set; } //
    public string? UpdatedBy { get; set; } //
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public virtual ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    public virtual ICollection<GRNHeader> GrnHeaders { get; set; } = new List<GRNHeader>();
    public bool CanBeDeleted()
    {
        // 1. Status Check: Sirf 'Draft' ya 'Pending' status wale PO delete hone chahiye
        // 'Completed', 'Partial', ya 'Invoiced' ko delete karna allow nahi hona chahiye
        var allowedStatuses = new[] { "Draft", "Pending" };

        if (!allowedStatuses.Contains(Status))
        {
            throw new Exception($"PO delete nahi ho sakta kyunki iska status '{Status}' hai. Sirf Draft ya Pending orders hi delete kiye ja sakte hain.");
        }

        // 2. Business Logic Check: Maan lo agar items locked hain ya processed hain
        // Yahan aap aur bhi conditions add kar sakte ho

        return true;
    }
    public void RecalculateTotals()
    {
        // Items table se naya total calculate karna
        this.TotalQuantity = this.Items.Sum(x => x.Qty);
        this.SubTotal = this.Items.Sum(x => x.Total - x.TaxAmount);
        this.TotalTax = this.Items.Sum(x => x.TaxAmount);
        this.GrandTotal = this.SubTotal + this.TotalTax;
        this.UpdatedDate = DateTime.UtcNow;
    }
}
