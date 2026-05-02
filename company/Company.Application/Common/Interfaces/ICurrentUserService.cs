using System;

namespace Company.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        Guid? CompanyId { get; }
        string? BranchId { get; }
        Guid? UserId { get; }
        bool IsSuperAdmin { get; }
        bool IsPlatformAdmin { get; }
    }
}
