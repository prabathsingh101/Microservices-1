using employeepayroll.Domain.Entities;
using employeepayroll.Application.Attendances.DTOs;
using MediatR;
using employeepayroll.Application.Common.Interfaces;

namespace employeepayroll.Application.Attendances.Commands;

public class SubmitAttendanceHandler(IEmployeePayrollDbContext context) : IRequestHandler<SubmitAttendanceCommand, Guid>
{
    public async Task<Guid> Handle(SubmitAttendanceCommand request, CancellationToken cancellationToken)
    {
        var attendance = new Attendance
        {
            EmployeeId = request.EmployeeId,
            Date = request.Date,
            CheckIn = request.CheckIn,
            CheckOut = request.CheckOut,
            Status = request.Status,
            Method = request.Method,
            Remarks = request.Remarks,
            CreatedOn = DateTime.UtcNow
        };

        context.Attendances.Add(attendance);
        await context.SaveChangesAsync(cancellationToken);

        return attendance.Id;
    }
}
