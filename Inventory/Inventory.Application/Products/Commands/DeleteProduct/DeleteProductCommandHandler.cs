using Inventory.Application.Common.Interfaces;
using MediatR;

namespace Inventory.Application.Products.Commands.DeleteProduct;

internal sealed class DeleteProductCommandHandler
    : IRequestHandler<DeleteProductCommand, Guid>
{
    private readonly IProductRepository _repository;
    private readonly IInventoryDbContext _context;

    public DeleteProductCommandHandler(
        IProductRepository repository,
        IInventoryDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<Guid> Handle(
        DeleteProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.Id);

        if (product is null)
            throw new KeyNotFoundException("Product not found");

       await _repository.DeleteAsync(product);

        await _context.SaveChangesAsync(cancellationToken);

        return request.Id;
    }
}
