using System;

namespace Inventory.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? CompanyId { get; }
    string? BranchId { get; }
    string? Email { get; }
}
