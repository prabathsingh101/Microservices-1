using Inventory.Application.Common.Interfaces;
using MediatR;

namespace Inventory.Application.PriceLists.Commands.DeletePriceList;

public sealed class DeletePriceListCommandHandler
    : IRequestHandler<DeletePriceListCommand, Guid>
{
    private readonly IPriceListRepository _repository;
    private readonly IInventoryDbContext _context;

    public DeletePriceListCommandHandler(
        IPriceListRepository repository,
        IInventoryDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<Guid> Handle(
        DeletePriceListCommand request,
        CancellationToken cancellationToken)
    {
        var priceList = await _repository.GetByIdAsync(request.Id);

        if (priceList is null)
            throw new KeyNotFoundException("PriceList not found");

        await _repository.DeleteAsync(priceList);

        await _context.SaveChangesAsync(cancellationToken);

        return request.Id;
    }
}
