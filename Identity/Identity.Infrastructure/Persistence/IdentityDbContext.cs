using Identity.Domain.Entities;
using Identity.Domain.Menus;
using Identity.Domain.Permissions;
using Identity.Domain.Roles;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Domain.Users.UserRole> UserRoles => Set<Domain.Users.UserRole>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Domain.PrintSettings.RolePrintSetting> RolePrintSettings => Set<Domain.PrintSettings.RolePrintSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PlanType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.PaymentTxnId).HasMaxLength(100);
        });

        modelBuilder.Entity<Menu>()
        .HasMany(m => m.Children)
        .WithOne(m => m.Parent)
        .HasForeignKey(m => m.ParentId); // Isse MenuId1 hat jayega

        modelBuilder.ApplyConfigurationsFromAssembly(
        typeof(IdentityDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
