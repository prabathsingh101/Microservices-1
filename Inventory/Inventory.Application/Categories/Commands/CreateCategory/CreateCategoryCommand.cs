using MediatR;

namespace Inventory.Application.Categories.Commands.CreateCategory;

public sealed record CreateCategoryCommand(
    string CategoryName,
    string? CategoryCode,
    decimal DefaultGst,
    string? Description,
    bool IsActive,
    Guid CompanyId
) : IRequest<Guid>;
