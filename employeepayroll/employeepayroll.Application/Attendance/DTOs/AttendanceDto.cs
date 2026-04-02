using employeepayroll.Domain.Enums;

namespace employeepayroll.Application.Attendances.DTOs;

public record AttendanceDto(
    Guid Id,
    Guid EmployeeId,
    string? EmployeeName,
    DateTime Date,
    DateTime? CheckIn,
    DateTime? CheckOut,
    AttendanceStatus Status,
    AttendanceMethod Method,
    string? Remarks
);

public record SubmitAttendanceCommand(
    Guid EmployeeId,
    DateTime Date,
    DateTime? CheckIn,
    DateTime? CheckOut,
    AttendanceStatus Status,
    AttendanceMethod Method,
    string? Remarks
) : MediatR.IRequest<Guid>;
