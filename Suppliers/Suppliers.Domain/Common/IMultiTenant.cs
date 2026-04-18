using System;

namespace Suppliers.Domain.Common;

public interface IMultiTenant
{
    Guid CompanyId { get; set; }
}
