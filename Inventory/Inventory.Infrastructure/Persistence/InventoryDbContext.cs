using Inventory.Domain.Entities;
using Inventory.Domain.Entities.SO;
using Inventory.Domain.PriceLists;
using Microsoft.EntityFrameworkCore;
using Inventory.Application.Common.Interfaces;

namespace Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContext : DbContext, IInventoryDbContext
{
    private readonly bool _isPlatformAdmin;
    private readonly Guid? _currentCompanyId;
    private readonly string? _currentBranchId;
    private readonly bool _isSuperAdmin;
    private readonly string? _currentUserEmail;

    public InventoryDbContext(
        DbContextOptions<InventoryDbContext> options,
        ICurrentUserService currentUserService)
        : base(options) 
    {
        // 🛡️ CAPTURE values here for stable Global Query Filter evaluation
        _isPlatformAdmin = currentUserService.IsPlatformAdmin;
        _isSuperAdmin = currentUserService.IsSuperAdmin;
        _currentCompanyId = currentUserService.CompanyId;
        _currentBranchId = currentUserService.BranchId;
        _currentUserEmail = currentUserService.Email;
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Subcategory> Subcategories => Set<Subcategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>(); 
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();  
    public DbSet<RequestForQuotation> RequestForQuotations => Set<RequestForQuotation>();
    public DbSet<RequestForQuotationItem> RequestForQuotationItems => Set<RequestForQuotationItem>();
    public DbSet<GRNHeader> GRNHeaders => Set<GRNHeader>(); 
    public DbSet<GRNDetail> GRNDetails => Set<GRNDetail>();
    public DbSet<SaleOrder> SaleOrders { get; set; }
    public DbSet<SaleOrderItem> SaleOrderItems { get; set; }
    public DbSet<Inventory.Domain.Entities.PurchaseReturn> PurchaseReturns { get; set; }
    public DbSet<Inventory.Domain.Entities.PurchaseReturnItem> PurchaseReturnItems { get; set; }
    public DbSet<SaleReturnHeader> SaleReturnHeaders { get; set; }
    public DbSet<SaleReturnItem> SaleReturnItems { get; set; }
    public DbSet<Inventory.Domain.Entities.SalesInvoice.SalesInvoice> SalesInvoices { get; set; }
    public DbSet<Inventory.Domain.Entities.SalesInvoice.SalesInvoiceItem> SalesInvoiceItems { get; set; }
    public DbSet<Inventory.Domain.Entities.SalesInvoice.DeliveryChallan> DeliveryChallans { get; set; }
    public DbSet<Inventory.Domain.Entities.SalesInvoice.DeliveryChallanItem> DeliveryChallanItems { get; set; }
    public DbSet<Inventory.Domain.Entities.SalesInvoice.SalesInvoiceDeliveryChallan> SalesInvoiceDeliveryChallans { get; set; }
    public DbSet<AppNotification> AppNotifications { get; set; }
    public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
    public DbSet<ExpenseEntry> ExpenseEntries { get; set; }
    public DbSet<ExpenseBudget> ExpenseBudgets { get; set; }
    public DbSet<GatePass> GatePasses => Set<GatePass>();
    public DbSet<UnitMaster> Units => Set<UnitMaster>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Rack> Racks => Set<Rack>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
    public DbSet<StockTransferHeader> StockTransferHeaders => Set<StockTransferHeader>();
    public DbSet<StockTransferDetail> StockTransferDetails => Set<StockTransferDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);

        modelBuilder.Entity<Inventory.Domain.Entities.SalesInvoice.SalesInvoiceDeliveryChallan>(entity =>
        {
            entity.HasKey(x => new { x.SalesInvoiceId, x.DeliveryChallanId });

            entity.HasOne(x => x.SalesInvoice)
                .WithMany()
                .HasForeignKey(x => x.SalesInvoiceId);

            entity.HasOne(x => x.DeliveryChallan)
                .WithMany()
                .HasForeignKey(x => x.DeliveryChallanId);
        });

        // ... [Existing entity configurations] ...

        // Apply Global Filters to all IMultiTenant entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Inventory.Domain.Common.IMultiTenant).IsAssignableFrom(entityType.ClrType))
            {
                if (entityType.ClrType == typeof(PriceList) || entityType.ClrType == typeof(PriceListItem))
                {
                    var method = typeof(InventoryDbContext)
                        .GetMethod(nameof(SetCompanyOnlyQueryFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.MakeGenericMethod(entityType.ClrType);
                    method?.Invoke(this, new object[] { modelBuilder });
                }
                else
                {
                    var method = typeof(InventoryDbContext)
                        .GetMethod(nameof(SetGlobalQueryFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.MakeGenericMethod(entityType.ClrType);
                    method?.Invoke(this, new object[] { modelBuilder });
                }
            }
        }
    }

    private void SetCompanyOnlyQueryFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, Inventory.Domain.Common.IMultiTenant
    {
        // 🚀 COMPANY-ONLY FILTER FOR GLOBAL PRICELISTS
        // Price lists are company-wide and should be shared across all branches.
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => 
            _isPlatformAdmin 
            ? true 
            : e.CompanyId == _currentCompanyId
        );
    }

    private void SetGlobalQueryFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, Inventory.Domain.Common.IMultiTenant
    {
        // 🚀 GLOBAL MULTI-TENANT FILTER
        // 1. If Platform Admin -> No Filter (they see all companies and all branches)
        // 2. Otherwise -> Filter by CompanyId AND (SuperAdmin OR specific BranchId)
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => 
            _isPlatformAdmin 
            ? true 
            : (e.CompanyId == _currentCompanyId && 
               (_isSuperAdmin || e.BranchId == null || string.IsNullOrEmpty(_currentBranchId) || _currentBranchId == "All Branches" || e.BranchId == _currentBranchId))
        );
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditAndTenantInfo();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditAndTenantInfo();
        return base.SaveChanges();
    }

    private void ApplyAuditAndTenantInfo()
    {
        var now = DateTime.UtcNow;
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            // 1. Audit Fields (CreatedOn, CreatedBy, etc.)
            if (entry.Entity is Inventory.Domain.Common.BaseAuditableEntity auditableEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    auditableEntity.CreatedOn = now;
                    auditableEntity.CreatedBy = _currentUserEmail;
                }
                
                auditableEntity.ModifiedOn = now;
                auditableEntity.ModifiedBy = _currentUserEmail;
            }

            // 2. Tenant Fields (CompanyId, BranchId)
            if (entry.Entity is Inventory.Domain.Common.IMultiTenant tenantEntity)
            {
                if (tenantEntity.CompanyId == Guid.Empty)
                    tenantEntity.CompanyId = _currentCompanyId ?? Guid.Empty;

                if (string.IsNullOrEmpty(tenantEntity.BranchId))
                    tenantEntity.BranchId = _currentBranchId;
            }
        }
    }
}
