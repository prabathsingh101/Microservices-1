using Inventory.Application.Common.Interfaces;
using MediatR;

namespace Inventory.Application.PriceLists.Commands.DeletePriceList
{

    internal sealed class BulkDeletePricelistsCommandHandler
     : IRequestHandler<BulkDeletePricelistsCommand>
    {
        private readonly IPriceListRepository _repository;
        private readonly IInventoryDbContext _context;

        public BulkDeletePricelistsCommandHandler(
            IPriceListRepository repository,
            IInventoryDbContext context)
        {
            _repository = repository;
            _context = context;
        }

        public async Task Handle(
            BulkDeletePricelistsCommand request,
            CancellationToken cancellationToken)
        {           

            var pricelists = await _repository.GetByIdsAsync(request.Ids);

            if (!pricelists.Any())
                throw new KeyNotFoundException("Price list not found");

            _repository.DeleteRange(pricelists);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
