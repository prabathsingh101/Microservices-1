using System;
using System.Collections.Generic;
using System.Text;

namespace Suppliers.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
