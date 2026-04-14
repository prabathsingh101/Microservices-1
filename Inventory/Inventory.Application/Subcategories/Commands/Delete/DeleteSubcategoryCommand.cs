using MediatR;

namespace Inventory.Application.Subcategories.Commands.Delete;

public sealed record DeleteSubcategoryCommand(Guid Id)
    : IRequest<Guid>;
