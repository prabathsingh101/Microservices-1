using Inventory.Domain.Entities;
using Inventory.Domain.Entities.SO;
using Inventory.Domain.PriceLists;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContext : DbContext,
    Application.Common.Interfaces.IInventoryDbContext
{
    private readonly Application.Common.Interfaces.ICurrentUserService _currentUserService;

    public InventoryDbContext(
        DbContextOptions<InventoryDbContext> options,
        Application.Common.Interfaces.ICurrentUserService currentUserService)
        : base(options) 
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Subcategory> Subcategories => Set<Subcategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>(); 
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();  
    
    public DbSet<GRNHeader> GRNHeaders => Set<GRNHeader>(); 

    public DbSet<GRNDetail> GRNDetails => Set<GRNDetail>();

    public DbSet<SaleOrder> SaleOrders { get; set; }
    public DbSet<SaleOrderItem> SaleOrderItems { get; set; }

    public DbSet<Inventory.Domain.Entities.PurchaseReturn> PurchaseReturns { get; set; }

    public DbSet<Inventory.Domain.Entities.PurchaseReturnItem> PurchaseReturnItems { get; set; }

    public DbSet<SaleReturnHeader> SaleReturnHeaders { get; set; }

    public DbSet<SaleReturnItem> SaleReturnItems { get; set; }

    public DbSet<AppNotification> AppNotifications { get; set; }
    public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
    public DbSet<ExpenseEntry> ExpenseEntries { get; set; }
    public DbSet<GatePass> GatePasses => Set<GatePass>();
    public DbSet<UnitMaster> Units => Set<UnitMaster>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Rack> Racks => Set<Rack>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);

        // PurchaseOrder Configuration
        modelBuilder.Entity<PurchaseOrder>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.PoNumber).IsRequired().HasMaxLength(50); //
            builder.Property(x => x.SubTotal).HasPrecision(18, 2);
            builder.Property(x => x.TotalTax).HasPrecision(18, 2);
            builder.Property(x => x.TotalQuantity).HasPrecision(18, 2);
            builder.Property(x => x.GrandTotal).HasPrecision(18, 2);
            builder.Property(x => x.IgstAmount).HasPrecision(18, 2);
            builder.Property(x => x.CgstAmount).HasPrecision(18, 2);
            builder.Property(x => x.SgstAmount).HasPrecision(18, 2);
            builder.Property(x => x.TcsAmount).HasPrecision(18, 2);
            builder.Property(x => x.TdsAmount).HasPrecision(18, 2);
            builder.Property(x => x.TcsPercent).HasPrecision(18, 2);
            builder.Property(x => x.TdsPercent).HasPrecision(18, 2);

            // Mapping Private Field for DDD
            builder.Metadata.FindNavigation(nameof(PurchaseOrder.Items))
                ?.SetPropertyAccessMode(PropertyAccessMode.Field);

            // One-to-Many Relationship (Fixed mapping to avoid shadow property PurchaseOrderId1)
            builder.HasMany(x => x.Items)
                   .WithOne(x => x.PurchaseOrder) // Back navigation specify ki taaki duplicate FK na bane
                   .HasForeignKey(x => x.PurchaseOrderId)
                   .OnDelete(DeleteBehavior.ClientCascade); // SQL Server cascade cycle error se bachne ke liye ClientCascade use karein
        });

        // Configuration for PurchaseReturn
        modelBuilder.Entity<PurchaseReturn>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReturnNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).HasDefaultValue("Draft"); // Default status set kiya hai
            entity.Property(e => e.SubTotal).HasPrecision(18, 2);
            entity.Property(e => e.TotalTax).HasPrecision(18, 2);
            entity.Property(e => e.GrandTotal).HasPrecision(18, 2);
        });

        // Relationship: One Return has many Items [cite: 2026-02-03]
        modelBuilder.Entity<PurchaseReturnItem>()
            .HasOne(p => p.PurchaseReturn)
            .WithMany(i => i.Items)
            .HasForeignKey(p => p.PurchaseReturnId)
            .OnDelete(DeleteBehavior.Cascade); // Header delete toh items bhi delete

        // PurchaseOrderItem Configuration
        modelBuilder.Entity<PurchaseOrderItem>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Qty).HasPrecision(18, 2);
            builder.Property(x => x.Rate).HasPrecision(18, 2);
            builder.Property(x => x.DiscountPercent).HasPrecision(18, 2);
            builder.Property(x => x.GstPercent).HasPrecision(18, 2);
            builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
            builder.Property(x => x.Total).HasPrecision(18, 2);
            builder.Property(x => x.ReceivedQty).HasPrecision(18, 2);
        });

        // PurchaseReturnItem Configuration
        modelBuilder.Entity<PurchaseReturnItem>(builder =>
        {
            builder.Property(x => x.ReturnQty).HasPrecision(18, 2);
            builder.Property(x => x.Rate).HasPrecision(18, 2);
            builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
            builder.Property(x => x.GstPercent).HasPrecision(18, 2);
            builder.Property(x => x.DiscountPercent).HasPrecision(18, 2);
            builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Product>(builder =>
        {
            builder.Property(x => x.BasePurchasePrice).HasPrecision(18, 2);
            builder.Property(x => x.MRP).HasPrecision(18, 2);
            builder.Property(x => x.Discount).HasPrecision(18, 2);
            builder.Property(x => x.SaleRate).HasPrecision(18, 2);
            builder.Property(x => x.DefaultGst).HasPrecision(18, 2);
            builder.Property(x => x.CurrentStock).HasPrecision(18, 2);
            builder.Property(x => x.DamagedStock).HasPrecision(18, 2);
        });

        // SaleReturn Configurations
        modelBuilder.Entity<SaleReturnHeader>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.SubTotal).HasPrecision(18, 2);
            builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
            builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            builder.Property(x => x.TotalAmount).HasPrecision(18, 2);

            builder.HasMany(x => x.ReturnItems)
                   .WithOne(x => x.SaleReturnHeader)
                   .HasForeignKey(x => x.SaleReturnHeaderId)
                   .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SaleReturnItem>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ReturnQty).HasPrecision(18, 2);
            builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
            builder.Property(x => x.DiscountPercent).HasPrecision(18, 2);
            builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            builder.Property(x => x.TaxPercentage).HasPrecision(18, 2);
            builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
            builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        });

        // SaleOrder aur SaleOrderItem ke beech Cascade Delete configuration
        modelBuilder.Entity<SaleOrderItem>()
           .HasOne(i => i.SaleOrder)
            .WithMany(o => o.Items)
            .HasForeignKey(i => i.SaleOrderId) 
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.HasOne(c => c.ParentCategory)
                  .WithMany(c => c.SubCategories)
                  .HasForeignKey(c => c.ParentCategoryId)
                  .OnDelete(DeleteBehavior.NoAction); // Cascade ki jagah NoAction use karein
        });



        modelBuilder.Entity<PriceListItem>()
        .HasOne(pi => pi.PriceList)
        .WithMany(pl => pl.PriceListItems)
        .HasForeignKey(pi => pi.PriceListId)
        .OnDelete(DeleteBehavior.NoAction); 

        // ExpenseCategory Configuration
        modelBuilder.Entity<ExpenseCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });

        // ExpenseEntry Configuration
        modelBuilder.Entity<ExpenseEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.PaymentMode).IsRequired().HasMaxLength(50);
            
            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Expenses)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // GatePass Configuration
        modelBuilder.Entity<GatePass>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PassNo).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.PassNo).IsUnique();
            entity.Property(e => e.TotalQty).HasPrecision(18, 2);
            entity.Property(e => e.TotalWeight).HasPrecision(18, 2);
        });

        // GRN configurations
        modelBuilder.Entity<GRNHeader>(entity =>
        {
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<GRNDetail>(entity =>
        {
            entity.Property(e => e.OrderedQty).HasPrecision(18, 2);
            entity.Property(e => e.PendingQty).HasPrecision(18, 2);
            entity.Property(e => e.RejectedQty).HasPrecision(18, 2);
            entity.Property(e => e.AcceptedQty).HasPrecision(18, 2);
            entity.Property(e => e.ReceivedQty).HasPrecision(18, 2);
            entity.Property(e => e.UnitRate).HasPrecision(18, 2);
            entity.Property(e => e.DiscountPercent).HasPrecision(18, 2);
            entity.Property(e => e.GstPercent).HasPrecision(18, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.Total).HasPrecision(18, 2);
        });

        modelBuilder.Entity<InventoryTransaction>(builder =>
        {
            builder.Property(x => x.Quantity).HasPrecision(18, 2);
        });

        modelBuilder.Entity<WarehouseStock>(builder =>
        {
            builder.Property(x => x.Quantity).HasPrecision(18, 2);
            builder.Property(x => x.MinStock).HasPrecision(18, 2);
            builder.HasIndex(x => new { x.ProductId, x.WarehouseId, x.CompanyId, x.BranchId })
                   .HasDatabaseName("IX_WarehouseStock_Main");
        });


        // --- 🔒 GLOBAL MULTI-TENANT FILTER & INDEXING ---
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Inventory.Domain.Common.IMultiTenant).IsAssignableFrom(entityType.ClrType))
            {
                // 1. Add Index on CompanyId for performance (Unique name to avoid collisions)
                modelBuilder.Entity(entityType.ClrType)
                    .HasIndex("CompanyId")
                    .HasDatabaseName("IX_MT_" + entityType.ClrType.Name + "_CompanyId");
                // 2. Set Global Query Filter
                var method = typeof(InventoryDbContext).GetMethod(nameof(SetGlobalQueryFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.MakeGenericMethod(entityType.ClrType);
                method?.Invoke(this, new object[] { modelBuilder });
            }
        }
    }

    private void SetGlobalQueryFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, Inventory.Domain.Common.IMultiTenant
    {
        var entityName = typeof(TEntity).Name;
        
        // Warehouses and Racks should be visible company-wide regardless of BranchId
        if (entityName == "Warehouse" || entityName == "Rack")
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(e => 
                e.CompanyId == _currentUserService.CompanyId);
        }
        else
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(e => 
                e.CompanyId == _currentUserService.CompanyId &&
                (e.BranchId == null || string.IsNullOrEmpty(_currentUserService.BranchId) || e.BranchId == _currentUserService.BranchId));
        }
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
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        DateTime utcNow = DateTime.UtcNow;
        var currentCompanyId = _currentUserService.CompanyId;

        foreach (var entry in entries)
        {
            var entity = entry.Entity;
            var type = entity.GetType();

            // Set CompanyId & BranchId for Multi-tenant entities
            if ((entry.State == EntityState.Added || entry.State == EntityState.Modified) && entity is Inventory.Domain.Common.IMultiTenant tenantEntity)
            {
                if (tenantEntity.CompanyId == Guid.Empty)
                {
                    tenantEntity.CompanyId = currentCompanyId ?? Guid.Empty;
                }

                // Automatically set BranchId only for transactional/stock entities
                // Masters like Product, Category, Warehouse should be Global (BranchId = null)
                var entityName = type.Name;
                var transactionalEntities = new[] { 
                    "WarehouseStock", "PurchaseOrder", "PurchaseOrderItem", 
                    "SaleOrder", "SaleOrderItem", "GRNHeader", "GRNDetail", 
                    "InventoryTransaction", "PurchaseReturn", "PurchaseReturnItem", 
                    "SaleReturnHeader", "SaleReturnItem", "ExpenseEntry",
                    "Warehouse", "Rack", "PriceList", "PriceListItem",
                    "Category", "Subcategory", "Product", "UnitMaster"
                };

                if (transactionalEntities.Contains(entityName))
                {
                    if (string.IsNullOrEmpty(tenantEntity.BranchId))
                    {
                        tenantEntity.BranchId = _currentUserService.BranchId;
                    }
                }
                else
                {
                    // Ensure BranchId is null for Global Masters
                    tenantEntity.BranchId = null;
                }
            }

            if (entry.State == EntityState.Added)
            {
                // ... same creation logic ...
                var createdProps = new[] { "CreatedOn", "CreatedOn", "CreatedAt", "CreatedOnTim" };
                foreach (var propName in createdProps)
                {
                    var prop = type.GetProperty(propName);
                    if (prop != null && prop.CanWrite)
                    {
                        var currentVal = prop.GetValue(entity);
                        if (currentVal == null || (currentVal is DateTime dt && (dt == default || dt.Year < 2000)))
                        {
                            prop.SetValue(entity, utcNow);
                        }
                    }
                }

                // Set CreatedBy
                var createdByProp = type.GetProperty("CreatedBy");
                if (createdByProp != null && createdByProp.CanWrite)
                {
                    createdByProp.SetValue(entity, _currentUserService.Email);
                }
            }

            // Set ModifiedBy
            var modifiedByProp = type.GetProperty("ModifiedBy");
            if (modifiedByProp != null && modifiedByProp.CanWrite)
            {
                modifiedByProp.SetValue(entity, _currentUserService.Email);
            }

            // ... same update logic ...
            var updateProps = new[] { "ModifiedOn", "ModifiedOn", "ModifiedOn", "UpdatedAt" };
            foreach (var propName in updateProps)
            {
                var prop = type.GetProperty(propName);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(entity, utcNow);
                }
            }
        }
    }
}
