using employeepayroll.Domain.Entities;
using MediatR;
using employeepayroll.Application.Employees.DTOs;
using employeepayroll.Application.Common.Interfaces;

namespace employeepayroll.Application.Employees.Commands;

public class CreateEmployeeHandler(IEmployeePayrollDbContext context) : IRequestHandler<CreateEmployeeCommand, Guid>
{
    public async Task<Guid> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = new Employee
        {
            EmployeeCode = request.EmployeeCode,
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Designation = request.Designation,
            Department = request.Department,
            DateOfJoining = request.DateOfJoining,
            ProfilePicture = request.ProfilePicture,
            BasicSalary = request.BasicSalary,
            HRA = request.HRA,
            Conveyance = request.Conveyance,
            SpecialAllowance = request.SpecialAllowance,
            PF = request.PF,
            Tax = request.Tax,
            IsActive = true
        };

        context.Employees.Add(employee);
        await context.SaveChangesAsync(cancellationToken);

        return employee.Id;
    }
}
