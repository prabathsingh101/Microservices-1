using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Inventory.Domain.Entities;

namespace Inventory.Application.Common.Interfaces
{
    public interface IStockTransferRepository
    {
        Task<string> CreateTransferAsync(StockTransferHeader header, List<StockTransferDetail> details);
        Task<IEnumerable<StockTransferHeader>> GetTransferListAsync();
        Task<StockTransferHeader?> GetTransferByIdAsync(Guid id);
        Task<bool> ReceiveTransferAsync(Guid id, string? remarks);
    }
}
