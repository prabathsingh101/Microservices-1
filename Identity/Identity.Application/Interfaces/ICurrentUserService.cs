using System;

namespace Identity.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? CompanyId { get; }
    string? BranchId { get; }
    Guid? UserId { get; }
    bool IsSuperAdmin { get; }
    bool IsPlatformAdmin { get; }
}
