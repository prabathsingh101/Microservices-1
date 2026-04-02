using Microsoft.EntityFrameworkCore;
using employeepayroll.Domain.Entities;
using employeepayroll.Application.Common.Interfaces;

namespace employeepayroll.Infrastructure.Persistence;

public class EmployeePayrollDBContext(DbContextOptions<EmployeePayrollDBContext> options) : DbContext(options), IEmployeePayrollDbContext
{
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<Leave> Leaves { get; set; }
    public DbSet<Holiday> Holidays { get; set; }
    public DbSet<SalarySlip> SalarySlips { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Fluent API configurations if needed
    }
}
