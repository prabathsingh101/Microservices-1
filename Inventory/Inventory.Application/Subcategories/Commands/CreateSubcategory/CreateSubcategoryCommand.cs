using MediatR;

namespace Inventory.Application.Subcategories.Commands.CreateSubcategory;

public sealed record CreateSubcategoryCommand(
    Guid categoryid,
    string? subcategorycode,
    string subcategoryname,
    decimal defaultgst,
    string? description,
    bool isactive,
    Guid CompanyId
) : IRequest<Guid>;
