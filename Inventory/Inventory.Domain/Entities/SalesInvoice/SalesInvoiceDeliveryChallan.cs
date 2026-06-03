using System;

namespace Inventory.Domain.Entities.SalesInvoice
{
    public class SalesInvoiceDeliveryChallan
    {
        public Guid SalesInvoiceId { get; set; }
        public Guid DeliveryChallanId { get; set; }

        // Navigation Properties
        public SalesInvoice? SalesInvoice { get; set; }
        public DeliveryChallan? DeliveryChallan { get; set; }
    }
}
