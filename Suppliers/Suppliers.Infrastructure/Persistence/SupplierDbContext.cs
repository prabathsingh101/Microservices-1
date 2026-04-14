using Suppliers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Suppliers.Application.Common.Interfaces;
using Suppliers.Domain.Common;
using Suppliers.Domain.Entities;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Suppliers.Infrastructure.Persistence
{
    public class SupplierDbContext : DbContext
    {
        private readonly ICurrentUserService? _currentUserService;

        public SupplierDbContext(
            DbContextOptions<SupplierDbContext> options,
            ICurrentUserService? currentUserService = null) 
            : base(options) 
        {
            _currentUserService = currentUserService;
        }

        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<SupplierPayment> SupplierPayments { get; set; }
        public DbSet<SupplierLedger> SupplierLedgers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Precision for decimals
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }

            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(15);
            });

            modelBuilder.Entity<SupplierPayment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PaymentMode).IsRequired().HasMaxLength(50);
            });

            modelBuilder.Entity<SupplierLedger>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TransactionType).IsRequired().HasMaxLength(50);
            });

            // --- 🔒 GLOBAL MULTI-TENANT FILTER ---
            if (_currentUserService != null)
            {
                foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                {
                    if (typeof(IMultiTenant).IsAssignableFrom(entityType.ClrType))
                    {
                        var method = typeof(SupplierDbContext).GetMethod(nameof(SetGlobalQueryFilter), BindingFlags.NonPublic | BindingFlags.Instance)
                            ?.MakeGenericMethod(entityType.ClrType);
                        method?.Invoke(this, new object[] { modelBuilder });
                    }
                }
            }
        }

        private void SetGlobalQueryFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, IMultiTenant
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.CompanyId == _currentUserService!.CompanyId);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditAndTenantInfo();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyAuditAndTenantInfo()
        {
            if (_currentUserService == null) return;

            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.Entity is BaseAuditableEntity auditableEntity)
                {
                    if (entry.State == EntityState.Added)
                    {
                        auditableEntity.CreatedOn = DateTime.UtcNow;
                        auditableEntity.CreatedBy = _currentUserService.UserId ?? "System";
                        if (auditableEntity.CompanyId == null || auditableEntity.CompanyId == Guid.Empty)
                        {
                            auditableEntity.CompanyId = _currentUserService.CompanyId;
                        }
                    }
                    else
                    {
                        auditableEntity.ModifiedOn = DateTime.UtcNow;
                        auditableEntity.ModifiedBy = _currentUserService.UserId ?? "System";
                    }
                }
            }
        }
    }
}
