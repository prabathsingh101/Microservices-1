using Company.Application.DTOs;
using MediatR;

namespace Company.Application.Company.Commands.Create
{
    public record CreateCompanyCommand(UpsertCompanyRequest Request) : IRequest<Guid>;
}