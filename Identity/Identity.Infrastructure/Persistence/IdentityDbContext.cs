using Identity.Domain;
using Identity.Domain.Common;
using Identity.Domain.Users;
using Identity.Domain.Roles;
using Identity.Domain.Menus;
using Identity.Domain.Permissions;
using Identity.Domain.Entities;
using Identity.Domain.PrintSettings;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Interfaces;

namespace Identity.Infrastructure.Persistence;

public class IdentityDbContext : DbContext
{
    private readonly bool _isPlatformAdmin;
    private readonly string? _currentBranchId;
    private readonly Guid? _currentCompanyId;
    private readonly string? _currentUserId;

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options, ICurrentUserService currentUserService)
        : base(options)
    {
        // 🛡️ CAPTURE values here for stable Global Query Filter evaluation
        _isPlatformAdmin = currentUserService.IsPlatformAdmin;
        _currentBranchId = currentUserService.BranchId;
        _currentCompanyId = currentUserService.CompanyId;
        _currentUserId = currentUserService.UserId?.ToString();
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Menu> Menus { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<PermissionAuditLog> PermissionAuditLogs { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<RolePrintSetting> RolePrintSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            entity.HasMany(u => u.UserRoles)
                  .WithOne(ur => ur.User)
                  .HasForeignKey(ur => ur.UserId)
                  .IsRequired();

            // 🛡️ FIX: Explicitly set backing field for private list
            entity.Navigation(u => u.UserRoles).HasField("_userRoles").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(r => r.Id);
        });

        builder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(ur => ur.Id);

            // 🛡️ FIX: Define relationship with Role
            entity.HasOne(ur => ur.Role)
                  .WithMany()
                  .HasForeignKey(ur => ur.RoleId);
        });

        builder.Entity<Menu>(entity =>
        {
            entity.ToTable("Menus");
            entity.HasKey(m => m.Id);
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(rp => rp.Id);
            entity.HasOne(rp => rp.Menu)
                  .WithMany()
                  .HasForeignKey(rp => rp.MenuId);
        });

        builder.Entity<Subscription>(entity =>
        {
            entity.ToTable("Subscriptions");
            entity.HasKey(s => s.Id);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(rt => rt.Id);
        });

        builder.Entity<RolePrintSetting>(entity =>
        {
            entity.ToTable("RolePrintSettings");
            entity.HasKey(rs => rs.Id);
        });

        builder.Entity<PermissionAuditLog>(entity =>
        {
            entity.ToTable("PermissionAuditLogs");
            entity.HasKey(pal => pal.Id);
        });

        // 🛡️ MULTI-TENANT GLOBAL QUERY FILTER
        builder.Entity<User>().HasQueryFilter(u =>
            _isPlatformAdmin ? true : u.CompanyId == _currentCompanyId
        );

        builder.Entity<PermissionAuditLog>().HasQueryFilter(pal =>
            _isPlatformAdmin ? true : pal.CompanyId == _currentCompanyId
        );

        builder.Entity<Subscription>().HasQueryFilter(s =>
            (_isPlatformAdmin && (string.IsNullOrEmpty(_currentBranchId) || _currentBranchId == "All Branches"))
            ? true
            : s.CompanyId == _currentCompanyId
        );
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        var user = _currentUserId ?? "System-Audit";
        var now = DateTime.UtcNow;

        // Use raw Entries() and check base type for maximum reliability
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditableEntity auditable && 
                (entry.State == EntityState.Added || entry.State == EntityState.Modified))
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedBy ??= user;
                    auditable.CreatedDate = now;
                }

                auditable.LastModifiedBy = user;
                auditable.LastModifiedDate = now;
                
                // 🚀 Explicitly mark as modified to ensure SQL UPDATE includes them
                entry.Property("LastModifiedBy").IsModified = true;
                entry.Property("LastModifiedDate").IsModified = true;
            }
        }
    }
}
