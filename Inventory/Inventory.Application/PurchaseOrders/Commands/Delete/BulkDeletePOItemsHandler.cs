// Application/Handlers/BulkDeletePOItemsHandler.cs
using Inventory.Application.Common.Interfaces;
using MediatR;

public class BulkDeletePOItemsHandler : IRequestHandler<BulkDeletePOItemsCommand, bool>
{
    private readonly IPurchaseOrderRepository _repo;
    private readonly IUnitOfWork _uow;

    public BulkDeletePOItemsHandler(IPurchaseOrderRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<bool> Handle(BulkDeletePOItemsCommand request, CancellationToken ct)
    {
        // 1. Parent PO ko items ke saath load karo
        var po = await _repo.GetByIdAsync(request.PurchaseOrderId);
        if (po == null) return false;

        // 2. Business Rule Check (Draft mode check)
        po.CanBeDeleted();

        // 3. Items ko find karo aur remove karo
        var itemsToRemove = po.Items.Where(x => request.ItemIds.Contains(x.Id)).ToList();

        foreach (var item in itemsToRemove)
        {
            po.Items.Remove(item);
        }

        // 4. DDD logic: Item delete hone ke baad total update karo
        po.RecalculateTotals();

        // 5. DB mein save karo
        return await _uow.SaveChangesAsync(ct) > 0;
    }
}
