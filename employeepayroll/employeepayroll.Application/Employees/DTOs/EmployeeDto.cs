namespace employeepayroll.Application.Employees.DTOs;

public record EmployeeDto(
    Guid Id,
    string EmployeeCode,
    string FullName,
    string Email,
    string Phone,
    string Designation,
    string Department,
    DateTime DateOfJoining,
    string ProfilePicture,
    decimal BasicSalary,
    decimal HRA,
    decimal GrossSalary,
    bool IsActive
);

public record CreateEmployeeCommand(
    string EmployeeCode,
    string FullName,
    string Email,
    string Phone,
    string Designation,
    string Department,
    DateTime DateOfJoining,
    string ProfilePicture,
    decimal BasicSalary,
    decimal HRA,
    decimal Conveyance,
    decimal SpecialAllowance,
    decimal PF,
    decimal Tax
) : MediatR.IRequest<Guid>;
