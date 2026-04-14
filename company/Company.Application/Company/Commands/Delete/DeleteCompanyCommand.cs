using MediatR;

namespace Company.Application.Company.Commands.Delete
{
    public record DeleteCompanyCommand(Guid Id) : IRequest<bool>;
}