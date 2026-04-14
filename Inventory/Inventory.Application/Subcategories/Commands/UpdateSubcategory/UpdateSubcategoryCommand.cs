using MediatR;

namespace Inventory.Application.Subcategories.Commands.UpdateSubcategory;

public sealed record UpdateSubcategoryCommand(
    Guid Id,
    Guid CategoryId,
    string Name,
    string? Code,
    decimal DefaultGst,
    string? Description,
    bool IsActive
) : IRequest<Guid>;
