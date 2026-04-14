using Company.Application.DTOs;
using MediatR;

namespace Company.Application.Company.Commands.Update
{
    public record UpdateCompanyCommand(Guid Id, UpsertCompanyRequest Request) : IRequest<Guid>;
}