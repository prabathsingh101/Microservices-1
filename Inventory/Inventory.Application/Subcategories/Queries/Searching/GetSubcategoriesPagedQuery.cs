using Inventory.Application.Categories.DTOs;
using Inventory.Application.Common.Models;
using Inventory.Application.Subcategories.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Subcategories.Queries.Searching
{
    public sealed record GetSubcategoriesPagedQuery(GridRequest Query)
    : IRequest<GridResponse<SubcategoryDto>>;

}
