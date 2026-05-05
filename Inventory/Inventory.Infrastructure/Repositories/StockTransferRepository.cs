using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories
{
    public class StockTransferRepository : IStockTransferRepository
    {
        private readonly InventoryDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public StockTransferRepository(InventoryDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<string> CreateTransferAsync(StockTransferHeader header, List<StockTransferDetail> details)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var companyId = _currentUserService.CompanyId ?? Guid.Empty;
                    
                    // 1. Generate Transfer Number if not provided
                    if (string.IsNullOrEmpty(header.TransferNumber))
                    {
                        var count = await _context.StockTransferHeaders.CountAsync(x => x.CompanyId == companyId);
                        header.SetTransferNumber($"TRF-{DateTime.Now.Year}-{(count + 1001)}");
                    }

                    await _context.StockTransferHeaders.AddAsync(header);
                    await _context.SaveChangesAsync();

                    foreach (var item in details)
                    {
                        item.StockTransferHeaderId = header.Id;
                        await _context.StockTransferDetails.AddAsync(item);

                        // 🚀 STOCK UPDATE LOGIC
                        
                        // A. Deduct from Source Warehouse
                        var sourceStock = await _context.WarehouseStocks
                            .FirstOrDefaultAsync(ws => ws.ProductId == item.ProductId && ws.WarehouseId == header.FromWarehouseId && ws.CompanyId == companyId);
                        
                        if (sourceStock == null || sourceStock.Quantity < item.Quantity)
                        {
                            throw new Exception($"Insufficient stock for Product ID {item.ProductId} in source warehouse.");
                        }
                        sourceStock.Quantity -= item.Quantity;

                        // B. Add to Destination Warehouse
                        var destStock = await _context.WarehouseStocks
                            .FirstOrDefaultAsync(ws => ws.ProductId == item.ProductId && ws.WarehouseId == header.ToWarehouseId && ws.CompanyId == companyId);
                        
                        if (destStock != null)
                        {
                            destStock.Quantity += item.Quantity;
                        }
                        else
                        {
                            await _context.WarehouseStocks.AddAsync(new WarehouseStock
                            {
                                ProductId = item.ProductId,
                                WarehouseId = header.ToWarehouseId,
                                Quantity = item.Quantity,
                                CompanyId = companyId,
                                BranchId = header.ToBranchId
                            });
                        }

                        // C. Record Inventory Transactions
                        // OUT from Source
                        await _context.InventoryTransactions.AddAsync(new InventoryTransaction(
                            item.ProductId,
                            -item.Quantity,
                            "Transfer-Out",
                            header.TransferNumber,
                            header.FromWarehouseId,
                            null, // Rack support can be added later
                            null, null,
                            companyId,
                            header.FromBranchId,
                            null, // ReferenceNumber not applicable here (Transfer)
                            item.BatchNumber
                        ));

                        // IN to Destination
                        await _context.InventoryTransactions.AddAsync(new InventoryTransaction(
                            item.ProductId,
                            item.Quantity,
                            "Transfer-In",
                            header.TransferNumber,
                            header.ToWarehouseId,
                            null,
                            null, null,
                            companyId,
                            header.ToBranchId,
                            null,
                            item.BatchNumber
                        ));
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return header.TransferNumber;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<IEnumerable<StockTransferHeader>> GetTransferListAsync()
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            return await _context.StockTransferHeaders
                .Include(h => h.FromWarehouse)
                .Include(h => h.ToWarehouse)
                .Where(h => h.CompanyId == companyId)
                .OrderByDescending(h => h.CreatedOn)
                .ToListAsync();
        }

        public async Task<StockTransferHeader?> GetTransferByIdAsync(Guid id)
        {
            return await _context.StockTransferHeaders
                .Include(h => h.FromWarehouse)
                .Include(h => h.ToWarehouse)
                .Include(h => h.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(h => h.Id == id);
        }
    }
}
