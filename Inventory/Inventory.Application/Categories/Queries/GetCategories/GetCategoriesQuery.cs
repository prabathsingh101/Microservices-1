using Inventory.Application.Categories.DTOs;
using Inventory.Application.Subcategories.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Categories.Queries.GetCategories
{
    public sealed record GetCategoriesQuery(Guid? CompanyId = null)
     : IRequest<List<CategoryDto>>;

}
