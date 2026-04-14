using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Services
{
    public interface ICustomerHttpService
    {
        Task<Dictionary<Guid, string>> GetCustomerNamesAsync(List<Guid> customerIds);
    }
}
