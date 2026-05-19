using Inventory.Application.Categories.Commands.DeleteCategory;
using Inventory.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Inventory.Application.Subcategories.Commands.Delete
{
    internal sealed class BulkDeleteSubCategoriesCommandHandler
     : IRequestHandler<BulkDeleteSubCategoriesCommand>
    {
        private readonly ISubcategoryRepository _repository;
        private readonly IInventoryDbContext _context;

        public BulkDeleteSubCategoriesCommandHandler(
            ISubcategoryRepository repository,
            IInventoryDbContext context)
        {
            _repository = repository;
            _context = context;
        }

        public async Task Handle(
            BulkDeleteSubCategoriesCommand request,
            CancellationToken cancellationToken)
        {
            if (request.Ids == null || request.Ids.Count == 0)
                throw new InvalidOperationException("No subcategories selected");

            var subcategories = await _repository.GetByIdsAsync(request.Ids);

            if (!subcategories.Any())
                throw new KeyNotFoundException("Subcategories not found");

            _repository.DeleteRange(subcategories);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
