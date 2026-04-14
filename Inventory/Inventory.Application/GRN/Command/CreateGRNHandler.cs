using Inventory.Application.Clients;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.GRN.Command;
using Inventory.Application.Services;
using Inventory.Domain.Entities;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

public class CreateGRNHandler : IRequestHandler<CreateGRNCommand, string>
{
    private readonly IGRNRepository _repo;
    private readonly IPurchaseOrderRepository _poRepo;
    private readonly IServiceScopeFactory _scopeFactory;

    public CreateGRNHandler(
        IGRNRepository repo,
        IPurchaseOrderRepository poRepo,
        IServiceScopeFactory scopeFactory)
    {
        _repo = repo;
        _poRepo = poRepo;
        _scopeFactory = scopeFactory;
    }

    public async Task<string> Handle(CreateGRNCommand request, CancellationToken ct)
    {
        var dto = request.Data;

        var header = new GRNHeader
        {
            GRNNumber = "AUTO-GEN",
            PurchaseOrderId = dto.POHeaderId,
            SupplierId = dto.SupplierId,
            ReceivedDate = dto.ReceivedDate,
            GatePassNo = dto.GatePassNo,
            TotalAmount = dto.TotalAmount,
            Remarks = dto.Remarks,
            CreatedBy = dto.CreatedBy,
            Status = "Received",
            ModifiedOn = DateTime.Now
        };

        var details = dto.Items.Select(i => new GRNDetail
        {
            ProductId = i.ProductId,
            OrderedQty = i.OrderedQty,
            PendingQty = i.PendingQty,
            ReceivedQty = i.ReceivedQty,
            RejectedQty = i.RejectedQty,
            AcceptedQty = i.AcceptedQty,
            UnitRate = i.UnitRate,
            DiscountPercent = i.DiscountPercent,
            GstPercent = i.GstPercent,
            TaxAmount = i.TaxAmount,
            Total = i.TotalAmount,
            WarehouseId = i.WarehouseId,
            RackId = i.RackId,
            MfgDate = i.ManufacturingDate,
            ExpDate = i.ExpiryDate,
            ModifiedOn = DateTime.Now
        }).ToList();

        var grnNumber = await _repo.SaveGRNWithStockUpdate(header, details);

        if (!string.IsNullOrEmpty(grnNumber))
        {
            // Background Task for notifications
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                var companyClient = scope.ServiceProvider.GetRequiredService<ICompanyClient>();
                var supplierClient = scope.ServiceProvider.GetRequiredService<ISupplierClient>();
                var poRepo = scope.ServiceProvider.GetRequiredService<IPurchaseOrderRepository>();

                try
                {
                    var company = await companyClient.GetCompanyProfileAsync();
                    var supplier = await supplierClient.GetSupplierByIdAsync(dto.SupplierId);
                    var po = await poRepo.GetByIdAsync(dto.POHeaderId);
                    string poNumber = po?.PoNumber ?? "N/A";

                    if (company != null && supplier != null)
                    {
                        // 1. Email
                        if (!string.IsNullOrEmpty(supplier.Email))
                        {
                            await emailService.SendGrnEmailAsync(company, supplier.Email, grnNumber, poNumber, dto.TotalAmount);
                        }

                        // 2. WhatsApp
                        if (!string.IsNullOrEmpty(supplier.Phone))
                        {
                            string msg = $"Goods Received: {grnNumber}\nRef PO: {poNumber}\nSource: {company.Name}\nStatus: Received & Accepted.\nThank you!";
                            await whatsAppService.SendMessageAsync(supplier.Phone, msg);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CreateGRNHandler] Notification failed: {ex.Message}");
                }
            });
        }

        return grnNumber;
    }
}
