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
            CompanyId = dto.CompanyId ?? Guid.Empty,
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
            CompanyId = dto.CompanyId ?? Guid.Empty,
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
            // 💰 1. Record Purchase in Supplier Ledger (CRITICAL - MUST BE SYNC)
            try
            {
                var supplierClient = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ISupplierClient>();
                string description = $"GRN: {grnNumber} for PO: {(dto.POHeaderId != Guid.Empty ? dto.POHeaderId : "Quick")}";
                await supplierClient.RecordPurchaseAsync(dto.SupplierId, dto.TotalAmount, grnNumber, description, dto.CreatedBy);
                Console.WriteLine($"[CreateGRNHandler] Purchase recorded for {grnNumber}");
            }
            catch (Exception ex)
            {
                // We log but don't fail the GRN if only the financial link fails, 
                // but usually it's better to log it clearly.
                Console.WriteLine($"[CreateGRNHandler] WARNING: Financial record failed: {ex.Message}");
            }

            // 📢 2. Background Task for non-critical notifications (Email/WhatsApp)
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
                    
                    if (company != null && supplier != null)
                    {
                        var poNumber = (dto.POHeaderId != Guid.Empty) ? (await poRepo.GetByIdAsync(dto.POHeaderId))?.PoNumber : "Quick";

                        // ✉️ Email
                        if (!string.IsNullOrEmpty(supplier.Email))
                        {
                            await emailService.SendGrnEmailAsync(company, supplier.Email, grnNumber, poNumber ?? "N/A", dto.TotalAmount);
                        }

                        // 📱 WhatsApp
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
