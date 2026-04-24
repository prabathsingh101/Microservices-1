using System;

namespace Suppliers.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        Guid? CompanyId { get; }
        Guid? BranchId { get; }
    }
}
