using System;

namespace Customers.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        Guid? CompanyId { get; }
    }
}
