using Customers.Application.Common.Interfaces;
using Customers.Domain.Common;
using Customers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Customers.Infrastructure.Persistence
{
    public class CustomerDbContext : DbContext
    {
        private readonly ICurrentUserService? _currentUserService;

        public CustomerDbContext(
            DbContextOptions<CustomerDbContext> options, 
            ICurrentUserService? currentUserService = null)
            : base(options) 
        {
            _currentUserService = currentUserService;
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerReceipt> CustomerReceipts { get; set; }
        public DbSet<CustomerLedger> CustomerLedgers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply specific configurations
            modelBuilder.ApplyConfiguration(new CustomerConfiguration());

            // Precision for decimals
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }

            modelBuilder.Entity<CustomerReceipt>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ReceiptMode).IsRequired().HasMaxLength(50);
            });

            modelBuilder.Entity<CustomerLedger>(entity =>
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
                        var method = typeof(CustomerDbContext).GetMethod(nameof(SetGlobalQueryFilter), BindingFlags.NonPublic | BindingFlags.Instance)
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
