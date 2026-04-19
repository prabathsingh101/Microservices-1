using System;

namespace Inventory.Domain.Common;

public interface IMultiTenant
{
    Guid CompanyId { get; set; }
}
