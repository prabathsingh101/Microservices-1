using Suppliers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Suppliers.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Suppliers.Infrastructure.Repositories
{
    internal class UnitOfWork:IUnitOfWork
    {
        private readonly SupplierDbContext _dbContext;
        public UnitOfWork(SupplierDbContext dbContext)
        {
             _dbContext=dbContext;
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
