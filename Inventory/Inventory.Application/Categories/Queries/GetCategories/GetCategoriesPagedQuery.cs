using Inventory.Application.Categories.DTOs;
using Inventory.Application.Common.Models;
using MediatR;

public sealed record GetCategoriesPagedQuery(GridRequest Query)
    : IRequest<GridResponse<CategoryDto>>;
