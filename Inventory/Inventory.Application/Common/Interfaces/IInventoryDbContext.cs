using DinkToPdf;
using Inventory.Domain.Entities;
using Inventory.Domain.Entities.SO;
using Inventory.Domain.PriceLists;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Inventory.Application.Common.Interfaces
{
    public interface IInventoryDbContext
    {
        DbSet<GRNHeader> GRNHeaders { get; }
        DbSet<GRNDetail> GRNDetails { get; }

        DbSet<SaleOrder> SaleOrders { get; }
        DbSet<SaleOrderItem> SaleOrderItems { get; }
        DbSet<Inventory.Domain.Entities.PurchaseReturn> PurchaseReturns { get; }
        DbSet<Inventory.Domain.Entities.PurchaseReturnItem> PurchaseReturnItems { get; }

        DbSet<SaleReturnHeader> SaleReturnHeaders { get; }
        DbSet<SaleReturnItem> SaleReturnItems { get; }
        DbSet<PriceList> PriceLists { get; }
        DbSet<PriceListItem> PriceListItems { get; }

        // PurchaseOrder Entities
        // Interface mein 'public' keyword hatayein
        DbSet<PurchaseOrder> PurchaseOrders { get; }
        DbSet<PurchaseOrderItem> PurchaseOrderItems { get; }

        public DbSet<AppNotification> AppNotifications { get; }

        DbSet<Product> Products { get; }
        DbSet<ExpenseCategory> ExpenseCategories { get; }
        DbSet<ExpenseEntry> ExpenseEntries { get; }
        DbSet<GatePass> GatePasses { get; }
        public DbSet<UnitMaster> Units { get; }
        DbSet<Warehouse> Warehouses { get; }
        DbSet<Rack> Racks { get; }
        DbSet<InventoryTransaction> InventoryTransactions { get; }
        DbSet<WarehouseStock> WarehouseStocks { get; }
        DbSet<StockTransferHeader> StockTransferHeaders { get; }
        DbSet<StockTransferDetail> StockTransferDetails { get; }

        // Is property se Handler ka error fix ho jayega
        DatabaseFacade Database { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
