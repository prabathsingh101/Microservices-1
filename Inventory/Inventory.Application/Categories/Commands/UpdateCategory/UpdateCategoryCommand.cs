using MediatR;

namespace Inventory.Application.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string? CategoryCode,
    decimal DefaultGst,
    string? Description,
    bool IsActive,
    Guid CompanyId
) : IRequest<Guid>;
