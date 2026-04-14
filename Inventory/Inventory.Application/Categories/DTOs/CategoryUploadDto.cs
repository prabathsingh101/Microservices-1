using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Categories.DTOs
{
    public class CategoryUploadDto
    {
        public string CategoryCode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal DefaultGst { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
