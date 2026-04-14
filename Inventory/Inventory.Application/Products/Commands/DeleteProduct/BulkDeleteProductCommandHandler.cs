using Inventory.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Products.Commands.DeleteProduct
{
    internal sealed class BulkDeleteProductCommandHandler
     : IRequestHandler<BulkDeleteProductCommand>
    {
        private readonly IProductRepository _repository;
        private readonly IInventoryDbContext _context;

        public BulkDeleteProductCommandHandler(
            IProductRepository repository,
            IInventoryDbContext context)
        {
            _repository = repository;
            _context = context;
        }

        public async Task Handle(
            BulkDeleteProductCommand request,
            CancellationToken cancellationToken)
        {

            var pricelists = await _repository.GetByIdsAsync(request.Ids);

            if (!pricelists.Any())
                throw new KeyNotFoundException("Product not found");

            _repository.DeleteRange(pricelists);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
