using Inventory.Application.Common.Interfaces;
using Inventory.Application.PurchaseOrders.DTOs;
using MediatR;

public class GetPOHeaderDetailsHandler : IRequestHandler<GetPOHeaderDetailsQuery, POHeaderDetailsDto>
{
    private readonly IPurchaseOrderRepository _repository;

    public GetPOHeaderDetailsHandler(IPurchaseOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<POHeaderDetailsDto> Handle(GetPOHeaderDetailsQuery request, CancellationToken ct)
    {
        // Repository se data fetch karna [cite: 2026-01-22]
        var po = await _repository.GetPOHeaderAsync(request.PurchaseOrderId);

        if (po == null) return null;

        return new POHeaderDetailsDto
        {
            SupplierId = po.SupplierId,  
            ProductId = po.ProductId,
            SupplierName = po.SupplierName,  
            PriceListId = po.PriceListId,    
            Remarks = po.Remarks,
            PoDate = DateTime.Now,  
            ExpectedDeliveryDate = po.ExpectedDeliveryDate,
        };
    }
}
