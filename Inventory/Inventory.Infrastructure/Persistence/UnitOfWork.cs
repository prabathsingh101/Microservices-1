using Inventory.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly InventoryDbContext _context;

        public UnitOfWork(InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync(CancellationToken ct)
        {
            // Sabhi pending changes (Header + Details) ko ek transaction mein save karega
            return await _context.SaveChangesAsync(ct);
        }
    }
}
