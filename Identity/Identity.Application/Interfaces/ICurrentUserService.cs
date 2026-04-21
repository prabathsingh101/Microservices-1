using System;

namespace Identity.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? CompanyId { get; }
    Guid? UserId { get; }
    bool IsSuperAdmin { get; }
}
