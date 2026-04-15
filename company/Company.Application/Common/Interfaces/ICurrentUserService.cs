using System;

namespace Company.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        Guid? CompanyId { get; }
        Guid? UserId { get; }
    }
}
