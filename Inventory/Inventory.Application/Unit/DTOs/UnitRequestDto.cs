using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Units.DTOs
{
    public class UnitRequestDto
    {
        // Unit ka naam (e.g., Kg, Pcs, Mtr)
        public string Name { get; set; }

        // Unit ki details (e.g., Kilogram, Pieces)
        public string Description { get; set; }

        // Optional: Agar aap edit ke waqt bhi yahi DTO use karna chahte hain
        public int? Id { get; set; }
    }
}
