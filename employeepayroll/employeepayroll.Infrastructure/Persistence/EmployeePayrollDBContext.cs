using Microsoft.EntityFrameworkCore;
using employeepayroll.Domain.Entities;
using employeepayroll.Application.Common.Interfaces;
using employeepayroll.Domain.Common;
using System.Reflection;

namespace employeepayroll.Infrastructure.Persistence;

public class EmployeePayrollDBContext : DbContext, IEmployeePayrollDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public EmployeePayrollDBContext(
        DbContextOptions<EmployeePayrollDBContext> options,
        ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<Leave> Leaves { get; set; }
    public DbSet<Holiday> Holidays { get; set; }
    public DbSet<SalarySlip> SalarySlips { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- 🔒 GLOBAL MULTI-TENANT FILTER ---
        if (_currentUserService != null)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(IMultiTenant).IsAssignableFrom(entityType.ClrType))
                {
                    var method = typeof(EmployeePayrollDBContext).GetMethod(nameof(SetGlobalQueryFilter), BindingFlags.NonPublic | BindingFlags.Instance)
                        ?.MakeGenericMethod(entityType.ClrType);
                    method?.Invoke(this, new object[] { modelBuilder });
                }
            }
        }
    }

    private void SetGlobalQueryFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, IMultiTenant
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            _currentUserService != null &&
            e.CompanyId == _currentUserService.CompanyId &&
            (e.BranchId == null || !_currentUserService.BranchId.HasValue || e.BranchId == _currentUserService.BranchId));
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedOn = DateTime.Now;
                    entry.Entity.CreatedBy = Guid.TryParse(_currentUserService?.UserId, out var createdBy) ? createdBy : null;
                    if (entry.Entity.CompanyId == null || entry.Entity.CompanyId == Guid.Empty)
                    {
                        entry.Entity.CompanyId = _currentUserService?.CompanyId;
                    }
                    if (entry.Entity.BranchId == null || entry.Entity.BranchId == Guid.Empty)
                    {
                        entry.Entity.BranchId = _currentUserService?.BranchId;
                    }
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedOn = DateTime.Now;
                    entry.Entity.ModifiedBy = Guid.TryParse(_currentUserService?.UserId, out var modifiedBy) ? modifiedBy : null;
                    if (entry.Entity.CompanyId == null || entry.Entity.CompanyId == Guid.Empty)
                    {
                        entry.Entity.CompanyId = _currentUserService?.CompanyId;
                    }
                    if (entry.Entity.BranchId == null || entry.Entity.BranchId == Guid.Empty)
                    {
                        entry.Entity.BranchId = _currentUserService?.BranchId;
                    }
                    break;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
