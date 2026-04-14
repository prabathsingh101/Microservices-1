using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.PurchaseOrders.DTOs
{
    public class PORepoPrintResponse
    {
        public byte[] PdfBytes { get; set; }
        public string HeaderTitle { get; set; }
    }
}
