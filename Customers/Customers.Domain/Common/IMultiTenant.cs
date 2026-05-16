using System;

namespace Customers.Domain.Common;

public interface IMultiTenant
{
    Guid? CompanyId { get; set; }
    string? BranchId { get; set; }
}
