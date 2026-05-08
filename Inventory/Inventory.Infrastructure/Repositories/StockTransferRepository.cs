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
        private readonly INotificationRepository _notificationRepository;

        public StockTransferRepository(
            InventoryDbContext context, 
            ICurrentUserService currentUserService,
            INotificationRepository notificationRepository)
        {
            _context = context;
            _currentUserService = currentUserService;
            _notificationRepository = notificationRepository;
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
                        var count = await _context.StockTransferHeaders.IgnoreQueryFilters().CountAsync(x => x.CompanyId == companyId);
                        header.SetTransferNumber($"TRF-{DateTime.Now.Year}-{(count + 1001)}");
                    }

                    await _context.StockTransferHeaders.AddAsync(header);
                    await _context.SaveChangesAsync();

                    foreach (var item in details)
                    {
                        item.StockTransferHeaderId = header.Id;
                        await _context.StockTransferDetails.AddAsync(item);

                        // 🚀 STOCK UPDATE LOGIC (Step 1: Deduct from Source Warehouse only)
                        
                        // A. Deduct from Source Warehouse
                        var sourceStock = await _context.WarehouseStocks
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(ws => ws.ProductId == item.ProductId && ws.WarehouseId == header.FromWarehouseId && ws.CompanyId == companyId);
                        
                        if (sourceStock == null || sourceStock.Quantity < item.Quantity)
                        {
                            throw new Exception($"Insufficient stock for Product ID {item.ProductId} in source warehouse.");
                        }
                        sourceStock.Quantity -= item.Quantity;

                        // B. Record Inventory Transactions (OUT from Source)
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
                    }

                    await _context.SaveChangesAsync();

                    // C. Add native In-App Notification for Destination Branch
                    await _notificationRepository.AddNotificationAsync(
                        "Incoming Stock Transfer",
                        $"New stock transfer {header.TransferNumber} dispatched to your branch from source branch.",
                        "Inventory",
                        "/app/inventory/item-transfer",
                        header.ToBranchId,
                        companyId
                    );

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

        public async Task<bool> ReceiveTransferAsync(Guid id, string? remarks)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var companyId = _currentUserService.CompanyId ?? Guid.Empty;

                    // 1. Fetch the transfer header with details
                    var transfer = await _context.StockTransferHeaders
                        .IgnoreQueryFilters()
                        .Include(h => h.Items)
                        .FirstOrDefaultAsync(h => h.Id == id && h.CompanyId == companyId);

                    if (transfer == null)
                    {
                        throw new Exception("Stock transfer record not found.");
                    }

                    // 2. Transition state and record remarks via clean domain method
                    transfer.ReceiveTransfer(remarks);

                    // 3. For each item, add to destination warehouse and record Transfer-In (Step 2: Add to Destination Warehouse)
                    foreach (var item in transfer.Items)
                    {
                        var destStock = await _context.WarehouseStocks
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(ws => ws.ProductId == item.ProductId && ws.WarehouseId == transfer.ToWarehouseId && ws.CompanyId == companyId);

                        if (destStock != null)
                        {
                            destStock.Quantity += item.Quantity;
                        }
                        else
                        {
                            await _context.WarehouseStocks.AddAsync(new WarehouseStock
                            {
                                ProductId = item.ProductId,
                                WarehouseId = transfer.ToWarehouseId,
                                Quantity = item.Quantity,
                                CompanyId = companyId,
                                BranchId = transfer.ToBranchId
                            });
                        }

                        // Record IN to Destination
                        await _context.InventoryTransactions.AddAsync(new InventoryTransaction(
                            item.ProductId,
                            item.Quantity,
                            "Transfer-In",
                            transfer.TransferNumber,
                            transfer.ToWarehouseId,
                            null,
                            null, null,
                            companyId,
                            transfer.ToBranchId,
                            null,
                            item.BatchNumber
                        ));
                    }

                    await _context.SaveChangesAsync();

                    // 4. Create AppNotification for the SOURCE branch to let them know it has been received
                    await _notificationRepository.AddNotificationAsync(
                        "Stock Transfer Received",
                        $"Stock transfer {transfer.TransferNumber} has been received successfully by the destination branch.",
                        "Inventory",
                        "/app/inventory/item-transfer",
                        transfer.FromBranchId,
                        companyId
                    );

                    await transaction.CommitAsync();
                    return true;
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
                .IgnoreQueryFilters()
                .Include(h => h.FromWarehouse)
                .Include(h => h.ToWarehouse)
                .Where(h => h.CompanyId == companyId)
                .OrderByDescending(h => h.CreatedOn)
                .ToListAsync();
        }

        public async Task<StockTransferHeader?> GetTransferByIdAsync(Guid id)
        {
            return await _context.StockTransferHeaders
                .IgnoreQueryFilters()
                .Include(h => h.FromWarehouse)
                .Include(h => h.ToWarehouse)
                .Include(h => h.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(h => h.Id == id);
        }
    }
}
