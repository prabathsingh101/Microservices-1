using employeepayroll.Application.Employees.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using employeepayroll.Application.Common.Interfaces;

namespace employeepayroll.Application.Employees.Queries;

public record GetEmployeesQuery() : IRequest<List<EmployeeDto>>;

public class GetEmployeesHandler(IEmployeePayrollDbContext context) : IRequestHandler<GetEmployeesQuery, List<EmployeeDto>>
{
    public async Task<List<EmployeeDto>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        return await context.Employees
            .Select(e => new EmployeeDto(
                e.Id,
                e.EmployeeCode ?? "",
                e.FullName ?? "",
                e.Email ?? "",
                e.Phone ?? "",
                e.Designation ?? "",
                e.Department ?? "",
                e.DateOfJoining,
                e.ProfilePicture ?? "",
                e.BasicSalary,
                e.HRA,
                (e.BasicSalary + e.HRA + e.Conveyance + e.SpecialAllowance),
                e.IsActive
            ))
            .ToListAsync(cancellationToken);
    }
}
