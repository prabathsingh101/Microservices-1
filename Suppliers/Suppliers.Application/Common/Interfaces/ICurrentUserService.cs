using System;

namespace Suppliers.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        Guid? CompanyId { get; }
        string? BranchId { get; }
    }
}
