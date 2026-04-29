using Identity.Domain.Entities;
using Identity.Domain.Menus;
using Identity.Domain.Permissions;
using Identity.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Identity.Infrastructure.Persistence;

public class IdentityDbContext : DbContext
{
    private readonly Application.Interfaces.ICurrentUserService _currentUserService;

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options, Application.Interfaces.ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Identity.Domain.User> Users => Set<Identity.Domain.User>();
    public DbSet<Identity.Domain.Roles.Role> Roles => Set<Identity.Domain.Roles.Role>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Identity.Domain.Users.UserRole> UserRoles => Set<Identity.Domain.Users.UserRole>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Domain.PrintSettings.RolePrintSetting> RolePrintSettings => Set<Domain.PrintSettings.RolePrintSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CompanyCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PlanType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.PaymentTxnId).HasMaxLength(100);

            entity.HasIndex(e => e.CompanyCode).IsUnique();
        });

        modelBuilder.Entity<Menu>()
        .HasMany(m => m.Children)
        .WithOne(m => m.Parent)
        .HasForeignKey(m => m.ParentId);

        modelBuilder.ApplyConfigurationsFromAssembly(
        typeof(IdentityDbContext).Assembly);

        // --- 🔒 GLOBAL MULTI-TENANT FILTERS ---
        
        // Roles and Permissions are Company-wide (Global to all branches)
        modelBuilder.Entity<Identity.Domain.Roles.Role>().HasQueryFilter(e => e.CompanyId == _currentUserService.CompanyId || e.CompanyId == null);
        modelBuilder.Entity<RolePermission>().HasQueryFilter(e => e.CompanyId == _currentUserService.CompanyId || e.CompanyId == null);
        
        // Users, RefreshTokens, and PrintSettings are isolated by CompanyId AND BranchId (unless Super Admin)
        modelBuilder.Entity<Identity.Domain.User>().HasQueryFilter(e => 
            e.CompanyId == _currentUserService.CompanyId && (_currentUserService.IsSuperAdmin || string.IsNullOrEmpty(_currentUserService.BranchId) || e.BranchId == _currentUserService.BranchId));
            
        modelBuilder.Entity<RefreshToken>().HasQueryFilter(e => 
            e.CompanyId == _currentUserService.CompanyId && (_currentUserService.IsSuperAdmin || string.IsNullOrEmpty(_currentUserService.BranchId) || e.BranchId == _currentUserService.BranchId));
            
        modelBuilder.Entity<Identity.Domain.PrintSettings.RolePrintSetting>().HasQueryFilter(e => 
            e.CompanyId == _currentUserService.CompanyId && (_currentUserService.IsSuperAdmin || string.IsNullOrEmpty(_currentUserService.BranchId) || e.BranchId == _currentUserService.BranchId));

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTenantInfo();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyTenantInfo();
        return base.SaveChanges();
    }

    private void ApplyTenantInfo()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        var currentCompanyId = _currentUserService.CompanyId;
        var currentBranchId = _currentUserService.BranchId;
        var currentUserId = _currentUserService.UserId;

        foreach (var entry in entries)
        {
            // 1. 🕒 AUDIT LOGIC (for AuditableEntity)
            if (entry.Entity is Domain.Common.AuditableEntity auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedDate = DateTime.UtcNow;
                    auditable.CreatedBy = currentUserId?.ToString();
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditable.LastModifiedDate = DateTime.UtcNow;
                    auditable.LastModifiedBy = currentUserId?.ToString();
                }
            }

            // 2. 🏢 TENANT LOGIC (for IMultiTenant)
            if (entry.Entity is Domain.Common.IMultiTenant tenantEntity)
            {
                // Only set CompanyId if it's missing AND we have a context value
                if ((tenantEntity.CompanyId == null || tenantEntity.CompanyId == Guid.Empty) && currentCompanyId.HasValue)
                {
                    tenantEntity.CompanyId = currentCompanyId;
                }

                var entityType = tenantEntity.GetType();
                bool isBranchSpecific = typeof(Identity.Domain.User).IsAssignableFrom(entityType) || 
                                       typeof(RefreshToken).IsAssignableFrom(entityType) || 
                                       typeof(Domain.Roles.Role).IsAssignableFrom(entityType) ||
                                       typeof(Domain.Permissions.RolePermission).IsAssignableFrom(entityType) ||
                                       typeof(Domain.Users.UserRole).IsAssignableFrom(entityType) ||
                                       typeof(Domain.Menus.Menu).IsAssignableFrom(entityType) ||
                                       typeof(Domain.PrintSettings.RolePrintSetting).IsAssignableFrom(entityType);

                if (isBranchSpecific)
                {
                    // Only set BranchId if it's missing AND we have a context value
                    if (string.IsNullOrEmpty(tenantEntity.BranchId) && !string.IsNullOrEmpty(currentBranchId))
                    {
                        tenantEntity.BranchId = currentBranchId;
                    }
                }
            }
        }
    }
}
