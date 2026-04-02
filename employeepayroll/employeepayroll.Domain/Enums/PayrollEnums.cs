namespace employeepayroll.Domain.Enums;

public enum AttendanceStatus
{
    Present,
    Absent,
    HalfDay,
    Leave,
    Late,
    Holiday
}

public enum AttendanceMethod
{
    Manual,
    Biometric
}

public enum LeaveStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}

public enum LeaveType
{
    Sick,
    Casual,
    Annual,
    Maternity,
    Paternity,
    Unpaid
}
