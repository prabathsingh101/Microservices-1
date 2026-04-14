using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.GRN.DTOs
{
    public class POForGRNDTO
    {
        public Guid POHeaderId { get; set; }
        public string PONumber { get; set; }
        public Guid SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string GrnNumber { get; set; }

        public string Remarks { get; set; }
        public List<POItemForGRNDTO> Items { get; set; }
    }
}
