using System;

namespace employeepayroll.Domain.Common
{
    public interface IMultiTenant
    {
        Guid? CompanyId { get; set; }
        Guid? BranchId { get; set; }
    }
}
