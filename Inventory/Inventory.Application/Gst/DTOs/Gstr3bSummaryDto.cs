using System;

namespace Inventory.Application.Gst.DTOs
{
    public class Gstr3bSummaryDto
    {
        public OutwardSuppliesDto OutwardSupplies { get; set; } = new();
        public InputTaxCreditDto InputTaxCredit { get; set; } = new();
        public NetPayableDto NetPayable { get; set; } = new();
    }

    public class OutwardSuppliesDto
    {
        // Table 3.1(a): Outward taxable supplies (other than zero rated, nil rated and exempted)
        public decimal TaxableValue { get; set; }
        public decimal IntegratedTax { get; set; }
        public decimal CentralTax { get; set; }
        public decimal StateTax { get; set; }
        public decimal Cess { get; set; }
    }

    public class InputTaxCreditDto
    {
        // Table 4(A)(5): All other ITC (from our purchases)
        public decimal TaxableValue { get; set; }
        public decimal IntegratedTax { get; set; }
        public decimal CentralTax { get; set; }
        public decimal StateTax { get; set; }
        public decimal Cess { get; set; }
    }

    public class NetPayableDto
    {
        // Output Liability - ITC
        public decimal IntegratedTax { get; set; }
        public decimal CentralTax { get; set; }
        public decimal StateTax { get; set; }
    }
}
