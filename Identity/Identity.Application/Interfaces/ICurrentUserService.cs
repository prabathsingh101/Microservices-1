using System;

namespace Identity.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? CompanyId { get; }
}
