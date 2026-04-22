using Identity.Domain.Entities;
using Identity.Domain.Menus;
using Identity.Domain.Permissions;
using Identity.Domain.Roles;
using Microsoft.EntityFrameworkCore;

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
        .HasForeignKey(m => m.ParentId); // Isse MenuId1 hat jayega

        modelBuilder.ApplyConfigurationsFromAssembly(
        typeof(IdentityDbContext).Assembly);

        modelBuilder.Entity<Identity.Domain.Roles.Role>().HasQueryFilter(e => e.CompanyId == _currentUserService.CompanyId || e.CompanyId == null);
        modelBuilder.Entity<RolePermission>().HasQueryFilter(e => e.CompanyId == _currentUserService.CompanyId || e.CompanyId == null);
        modelBuilder.Entity<RefreshToken>().HasQueryFilter(e => e.CompanyId == _currentUserService.CompanyId || e.CompanyId == null);
        modelBuilder.Entity<Identity.Domain.PrintSettings.RolePrintSetting>().HasQueryFilter(e => e.CompanyId == _currentUserService.CompanyId || e.CompanyId == null);

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
            .Where(e => e.State == EntityState.Added && e.Entity is Domain.Common.IMultiTenant);

        var currentCompanyId = _currentUserService.CompanyId;

        foreach (var entry in entries)
        {
            var tenantEntity = (Domain.Common.IMultiTenant)entry.Entity;
            if (tenantEntity.CompanyId == null || tenantEntity.CompanyId == Guid.Empty)
            {
                tenantEntity.CompanyId = currentCompanyId;
            }
        }
    }
}
