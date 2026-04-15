using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.GRN.DTOs
{
    public class SaveGRNCommandDTO
    {
        public Guid POHeaderId { get; set; }
        public Guid SupplierId { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string? GatePassNo { get; set; }
        public string Remarks { get; set; }
        public decimal TotalAmount { get; set; }
        public string CreatedBy { get; set; }   
        public Guid? CompanyId { get; set; }
        public List<GRNItemDTO> Items { get; set; }
    }
}
