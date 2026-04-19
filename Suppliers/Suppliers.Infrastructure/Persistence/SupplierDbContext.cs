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
            modelBuilder.Entity<TEntity>().HasQueryFilter(e => 
                e.CompanyId == null || 
                e.CompanyId == Guid.Empty || 
                (_currentUserService != null && e.CompanyId == _currentUserService.CompanyId));
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedOn = DateTime.Now;
                        entry.Entity.CreatedBy = _currentUserService?.UserId;
                        if (entry.Entity.CompanyId == null || entry.Entity.CompanyId == Guid.Empty)
                        {
                            entry.Entity.CompanyId = _currentUserService?.CompanyId;
                        }
                        break;
                    case EntityState.Modified:
                        entry.Entity.ModifiedOn = DateTime.Now;
                        entry.Entity.ModifiedBy = _currentUserService?.UserId;
                        // Always claim legacy data when modified
                        if (entry.Entity.CompanyId == null || entry.Entity.CompanyId == Guid.Empty)
                        {
                            entry.Entity.CompanyId = _currentUserService?.CompanyId;
                        }
                        break;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
