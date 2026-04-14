// Application/Handlers/DeletePurchaseOrderHandler.cs
using Inventory.Application.Common.Interfaces;
using MediatR;

public class DeletePurchaseOrderHandler : IRequestHandler<DeletePurchaseOrderCommand, bool>
{
    private readonly IPurchaseOrderRepository _repo;
    private readonly IUnitOfWork _uow;

    public DeletePurchaseOrderHandler(IPurchaseOrderRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<bool> Handle(DeletePurchaseOrderCommand request, CancellationToken ct)
    {
        var po = await _repo.GetByIdAsync(request.Id);
        if (po == null) return false;

        // Business Rule Check
        po.CanBeDeleted();

        _repo.Delete(po);
        return await _uow.SaveChangesAsync(ct) > 0;
    }
}
