using Inventory.Application.Clients;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.PurchaseOrders.Queries.GetNextPoNumber;
using Inventory.Application.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class CreatePurchaseOrderCommandHandler : IRequestHandler<CreatePurchaseOrderCommand, bool>
{
    private readonly IInventoryDbContext _context;
    private readonly IPurchaseOrderRepository _repo;
    private readonly IMediator _mediator;
    private readonly IServiceScopeFactory _scopeFactory;

    public CreatePurchaseOrderCommandHandler(
        IInventoryDbContext context, 
        IPurchaseOrderRepository repo, 
        IMediator mediator,
        IServiceScopeFactory scopeFactory)
    {
        _context = context;
        _repo = repo;
        _mediator = mediator;
        _scopeFactory = scopeFactory;
    }

    public async Task<bool> Handle(CreatePurchaseOrderCommand request, CancellationToken ct)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        string? finalPoNumber = null;

        var result = await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var dto = request.PoData;

                // Calling your existing PO generation logic
                string generatedPoNumber = await _mediator.Send(new GetNextPoNumberQuery(), ct);

                var po = new PurchaseOrder
                {
                    PoNumber = generatedPoNumber,
                    SupplierId = dto.SupplierId,
                    SupplierName = dto.SupplierName,
                    PriceListId = dto.PriceListId,
                    PoDate = dto.PoDate,
                    TotalQuantity = dto.TotalQuantity,
                    TotalTax = dto.TotalTax,
                    SubTotal = dto.SubTotal,
                    GrandTotal = dto.GrandTotal,
                    TaxType = dto.TaxType,
                    TdsPercent = dto.TdsPercent,
                    TdsAmount = dto.TdsAmount,
                    TcsPercent = dto.TcsPercent,
                    TcsAmount = dto.TcsAmount,
                    IgstAmount = dto.IgstAmount,
                    CgstAmount = dto.CgstAmount,
                    SgstAmount = dto.SgstAmount,
                    CreatedBy = dto.CreatedBy,
                    Remarks = dto.Remarks,
                    ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
                    Items = dto.Items.Select(i => new PurchaseOrderItem
                    {
                        ProductId = i.ProductId,
                        Qty = i.Qty,
                        Unit = i.Unit,
                        Rate = i.Rate,
                        DiscountPercent = i.DiscountPercent,
                        GstPercent = i.GstPercent,
                        TaxAmount = i.TaxAmount,
                        Total = i.Total,
                        MfgDate = i.ManufacturingDate,
                        ExpDate = i.ExpiryDate
                    }).ToList()
                };

                await _repo.AddAsync(po, ct);
                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                finalPoNumber = generatedPoNumber;
                return true;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
        return result;
    }
}
