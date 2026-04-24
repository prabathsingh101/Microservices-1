using System;

namespace Identity.Domain.Common;

public interface IMultiTenant
{
    Guid? CompanyId { get; set; }
    string? BranchId { get; set; }
}
