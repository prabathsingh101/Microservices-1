using employeepayroll.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace employeepayroll.Application.Common.Interfaces;

public interface IEmployeePayrollDbContext
{
    DbSet<Employee> Employees { get; }
    DbSet<Attendance> Attendances { get; }
    DbSet<Leave> Leaves { get; }
    DbSet<Holiday> Holidays { get; }
    DbSet<SalarySlip> SalarySlips { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
