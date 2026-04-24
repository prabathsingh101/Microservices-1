using Inventory.Application.Common.Interfaces;
using Inventory.Application.SaleOrders.Commands;
using Inventory.Application.Clients;
using Inventory.Application.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Inventory.Domain.Entities.SO;
using Inventory.Domain.Entities;

public class CreateSaleOrderHandler : IRequestHandler<CreateSaleOrderCommand, object>
{
    private readonly ISaleOrderRepository _repo;
    private readonly IInventoryDbContext _context;
    private readonly ICustomerClient _customerClient;
    private readonly IServiceScopeFactory _scopeFactory;

    public CreateSaleOrderHandler(
        ISaleOrderRepository repo, 
        IInventoryDbContext context, 
        IServiceScopeFactory scopeFactory,
        ICustomerClient customerClient)
    {
        _repo = repo;
        _context = context;
        _scopeFactory = scopeFactory;
        _customerClient = customerClient;
    }

    public async Task<object> Handle(CreateSaleOrderCommand request, CancellationToken cancellationToken)
    {
        var dto = request.OrderDto;
        bool isEdit = dto.Id != Guid.Empty;
        string? existingSONo = null;
        string? oldStatus = null;

        if (isEdit)
        {
            var existing = await _context.SaleOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dto.Id);
            if (existing != null)
            {
                existingSONo = existing.SONumber;
                oldStatus = existing.Status;
            }
        }

        // 1. SONumber Setup
        string finalSONo = existingSONo;
        if (string.IsNullOrEmpty(finalSONo))
        {
            string lastNo = await _repo.GetLastSONumberAsync();
            int nextId = lastNo == null ? 1 : int.Parse(lastNo.Split('-').Last()) + 1;
            
            string prefix = dto.IsQuick ? "SO-Q" : "SO";
            finalSONo = $"{prefix}-{DateTime.Now.Year}-{nextId:D4}";
        }

        // 2. SaleOrder Object Mapping
        var saleOrder = new SaleOrder
        {
            Id = dto.Id,
            CompanyId = dto.CompanyId,
            BranchId = dto.BranchId,
            SONumber = finalSONo,
            CustomerId = dto.CustomerId,
            SODate = dto.SoDate,
            ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
            SubTotal = dto.SubTotal,
            TotalTax = dto.TotalTax,
            GrandTotal = dto.GrandTotal,
            TaxType = dto.TaxType,
            TdsPercent = dto.TdsPercent,
            TdsAmount = dto.TdsAmount,
            TcsPercent = dto.TcsPercent,
            TcsAmount = dto.TcsAmount,
            IgstAmount = dto.IgstAmount,
            CgstAmount = dto.CgstAmount,
            SgstAmount = dto.SgstAmount,
            Remarks = dto.Remarks,
            Status = dto.Status,
            CreatedBy = dto.CreatedBy,
            IsQuick = dto.IsQuick, // Map flag from DTO
            Items = dto.Items.Select(i => new SaleOrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Qty = i.Qty,
                Unit = i.Unit,
                Rate = i.Rate,
                MRP = i.MRP,
                DiscountAmount = i.DiscountAmount,
                DiscountPercent = i.DiscountPercent,
                GSTPercent = i.GstPercent,
                TaxAmount = i.TaxAmount,
                Total = i.Total,
                MfgDate = i.ManufacturingDate,
                ExpDate = i.ExpiryDate,
                WarehouseId = i.WarehouseId,
                RackId = i.RackId,
                CompanyId = dto.CompanyId,
                BranchId = dto.BranchId
            }).ToList()
        };

        bool shouldProcessConfirmed = (dto.Status == "Confirmed");
        object? result = null;

        if (shouldProcessConfirmed)
        {
            await _repo.ExecuteInTransactionAsync(async () =>
            {
                decimal oldGrandTotal = 0;
                if (isEdit && oldStatus == "Confirmed")
                {
                    // 1. Revert Old Stock and Old Ledger
                    var existingWithItems = await _context.SaleOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == dto.Id);
                    if (existingWithItems != null)
                    {
                        oldGrandTotal = existingWithItems.GrandTotal;
                        foreach (var item in existingWithItems.Items)
                        {
                            await _repo.UpdateProductStockAsync(item.ProductId, item.Qty);

                            // 🆕 Record Reversal in Audit Trail
                            var reversalTx = new InventoryTransaction(
                                item.ProductId,
                                item.Qty, // Positive because it is READDING stock
                                (existingWithItems.IsQuick ? "QuickSale" : "Sale") + "-REVERSAL",
                                existingWithItems.SONumber,
                                item.WarehouseId,
                                item.RackId,
                                item.MfgDate,
                                item.ExpDate,
                                existingWithItems.CompanyId
                            );
                            await _context.InventoryTransactions.AddAsync(reversalTx);
                        }

                        // Optional: Record reversal for OLD amount before recording NEW
                        try
                        {
                            await _customerClient.RecordSaleAsync(
                                existingWithItems.CustomerId,
                                -existingWithItems.GrandTotal,
                                existingWithItems.SONumber,
                                $"Sale Order Adjustment (Old Reversal): {existingWithItems.SONumber}",
                                "System"
                            );
                        }
                        catch (Exception ex) { Console.WriteLine($"Old Ledger reversal failed: {ex.Message}"); }
                    }
                }

                Guid savedId;
                if (isEdit)
                {
                    await _repo.UpdateAsync(saleOrder);
                    savedId = saleOrder.Id;
                }
                else
                {
                    savedId = await _repo.SaveAsync(saleOrder);
                }

                // 2. Deduct New Stock
                foreach (var item in saleOrder.Items)
                {
                    decimal availableStock = await _repo.GetAvailableStockAsync(item.ProductId);
                    if (availableStock < item.Qty)
                    {
                        throw new Exception($"Insufficient stock for {item.ProductName}. Available: {availableStock}");
                    }
                    await _repo.UpdateProductStockAsync(item.ProductId, -item.Qty);

                    // 🆕 Record Inventory Transaction
                    var saleTx = new InventoryTransaction(
                        item.ProductId,
                        -item.Qty, // Negative because it is REDUCING stock
                        saleOrder.IsQuick ? "QuickSale" : "Sale",
                        saleOrder.SONumber,
                        item.WarehouseId,
                        item.RackId,
                        item.MfgDate,
                        item.ExpDate,
                        saleOrder.CompanyId
                    );
                    await _context.InventoryTransactions.AddAsync(saleTx);
                }

                // 3. Record New Ledger
                try
                {
                    await _customerClient.RecordSaleAsync(
                        saleOrder.CustomerId,
                        saleOrder.GrandTotal,
                        saleOrder.SONumber,
                        $"Sale Invoice generated: {saleOrder.SONumber}",
                        saleOrder.CreatedBy ?? "System"
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ledger sync failed: {ex.Message}");
                }

                result = new { Id = savedId, SONumber = finalSONo };
            });
        }
        else
        {
            // Simple Save/Update without stock deduction
            if (isEdit)
            {
                await _repo.UpdateAsync(saleOrder);
                result = new { Id = saleOrder.Id, SONumber = finalSONo };
            }
            else
            {
                var savedId = await _repo.SaveAsync(saleOrder);
                result = new { Id = savedId, SONumber = finalSONo };
            }
        }

        // 4. Notifications
        if (result != null && dto.Status == "Confirmed" && (oldStatus == null || oldStatus != "Confirmed"))
        {
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                var companyClient = scope.ServiceProvider.GetRequiredService<ICompanyClient>();
                var customerClient = scope.ServiceProvider.GetRequiredService<ICustomerClient>();

                try
                {
                    var company = await companyClient.GetCompanyProfileAsync();
                    var customer = await customerClient.GetCustomerByIdAsync(saleOrder.CustomerId);

                    if (company != null && customer != null)
                    {
                        if (!string.IsNullOrEmpty(customer.Email))
                        {
                            await emailService.SendSoEmailAsync(company, customer.Email, finalSONo, saleOrder.GrandTotal);
                        }
                        if (!string.IsNullOrEmpty(customer.Phone))
                        {
                            string msg = $"Order Confirmed! 🚀\nFrom: {company.Name}\nOrder No: {finalSONo}\nAmount: {saleOrder.GrandTotal}\nThank you for shopping with us!";
                            await whatsAppService.SendMessageAsync(customer.Phone, msg);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CreateSaleOrderHandler] Notification task failed: {ex.Message}");
                }
            });
        }

        return result;
    }
}
