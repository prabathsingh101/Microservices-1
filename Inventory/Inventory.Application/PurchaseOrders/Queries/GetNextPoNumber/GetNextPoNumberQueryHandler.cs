using Inventory.Application.Common.Interfaces;
using Inventory.Application.PurchaseOrders.Queries.GetNextPoNumber;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Queries.GetNextPoNumber
{
    public class GetNextPoNumberQueryHandler : IRequestHandler<GetNextPoNumberQuery, string>
    {
        private readonly IInventoryDbContext _context; // Direct DBContext ya Repository use karein

        public GetNextPoNumberQueryHandler(IInventoryDbContext context)
        {
            _context = context;
        }

        public async Task<string> Handle(GetNextPoNumberQuery request, CancellationToken cancellationToken)
        {
            // If request.IsQuick is true, prefix is "QPO", else "PO"
            string prefix = request.IsQuick ? "QPO" : "PO";

            // 1. Database se last PO Number nikalein jo matching prefix ke sath ho
            var lastPoNumber = await _context.PurchaseOrders
                .Where(p => p.PoNumber.StartsWith(prefix + "/"))
                .OrderByDescending(p => p.CreatedOn)
                .Select(p => p.PoNumber)
                .FirstOrDefaultAsync(cancellationToken);

            int nextId = 1;

            // 2. Agar koi purana PO mila hai toh uska number extract karein
            if (!string.IsNullOrEmpty(lastPoNumber))
            {
                // Format assumed: PREFIX/25-26/0001
                var parts = lastPoNumber.Split('/');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastId))
                {
                    nextId = lastId + 1;
                }
            }

            // 3. Current Financial Year (is saal ka format)
            string finYear = "26-27";

            // 4. Final String return karein (D4 means 0001 format)
            return $"{prefix}/{finYear}/{nextId:D4}";
        }
    }
}
