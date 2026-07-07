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
            BranchId = dto.BranchId,
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
            BranchId = dto.BranchId,
            IsReplacement = i.IsReplacement,
            ModifiedOn = DateTime.Now
        }).ToList();

        var grnNumber = await _repo.SaveGRNWithStockUpdate(header, details);

        if (!string.IsNullOrEmpty(grnNumber))
        {
            // 💰 1. Record Purchase in Supplier Ledger (CRITICAL - MUST BE SYNC)
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var supplierClient = scope.ServiceProvider.GetRequiredService<ISupplierClient>();
                var poRepo = scope.ServiceProvider.GetRequiredService<IPurchaseOrderRepository>();

                string poDisplay = "Quick";
                try
                {
                    var po = (dto.POHeaderId != Guid.Empty) ? await poRepo.GetByIdAsync(dto.POHeaderId) : null;
                    if (po != null) poDisplay = po.PoNumber;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CreateGRNHandler] Warning: PO query failed: {ex.Message}");
                }

                string supplierDisplay = "N/A";
                try
                {
                    var supplier = await supplierClient.GetSupplierByIdAsync(dto.SupplierId);
                    if (supplier != null) supplierDisplay = supplier.Name;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CreateGRNHandler] Warning: Supplier query failed: {ex.Message}");
                }

                // 🛡️ REPLACEMENT AUTO-OFFSET: Calculate original value of replacement items
                decimal replacementValue = 0;
                if (dto.Items != null)
                {
                    foreach (var item in dto.Items)
                    {
                        if (item.IsReplacement)
                        {
                            decimal baseAmt = item.AcceptedQty * item.UnitRate;
                            decimal discAmt = baseAmt * (item.DiscountPercent / 100m);
                            decimal taxableAmt = baseAmt - discAmt;
                            decimal taxAmt = taxableAmt * (item.GstPercent / 100m);
                            replacementValue += taxableAmt + taxAmt;
                        }
                    }
                }
                decimal ledgerAmount = dto.TotalAmount + replacementValue;
                ledgerAmount = Math.Round(ledgerAmount, 2, MidpointRounding.AwayFromZero);

                string description = $"GRN: {grnNumber} for PO: {poDisplay} ({supplierDisplay})";
                if (replacementValue > 0)
                {
                    description += $" (Includes Auto-Offset Replacement: {replacementValue:0.00})";
                }

                await supplierClient.RecordPurchaseAsync(dto.SupplierId, ledgerAmount, grnNumber, description, dto.CreatedBy);
                Console.WriteLine($"[CreateGRNHandler] Purchase recorded for {grnNumber} with ledger amount: {ledgerAmount}");
            }
            catch (Exception ex)
            {
                // We log but don't fail the GRN if only the financial link fails, 
                // but usually it's better to log it clearly.
                Console.WriteLine($"[CreateGRNHandler] WARNING: Financial record failed: {ex.Message}");
            }

            // FETCH DATA BEFORE Task.Run SO HTTP CONTEXT IS AVAILABLE FOR TOKENS
            var companyClientNotification = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ICompanyClient>();
            var supplierClientNotification = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ISupplierClient>();
            var poRepoLocal = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IPurchaseOrderRepository>();
            
            Inventory.Application.Clients.DTOs.CompanyProfileDto? companyNotification = null;
            Inventory.Application.PurchaseReturn.SupplierSelectDto? supplierNotification = null;
            string? poNumberLocal = "Quick";
            
            try 
            {
                companyNotification = await companyClientNotification.GetCompanyProfileAsync();
                supplierNotification = await supplierClientNotification.GetSupplierByIdAsync(dto.SupplierId);
                poNumberLocal = (dto.POHeaderId != Guid.Empty) ? (await poRepoLocal.GetByIdAsync(dto.POHeaderId))?.PoNumber : "Quick";
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[CreateGRNHandler] Failed to fetch data for notification: {ex.Message}");
            }

            // 📢 2. Background Task for non-critical notifications (Email/WhatsApp)
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

                try
                {
                    if (companyNotification != null && supplierNotification != null)
                    {
                        // ✉️ Email
                        if (!string.IsNullOrEmpty(supplierNotification.Email))
                        {
                            byte[] pdfBytes = null;
                            if (dto.POHeaderId != Guid.Empty)
                            {
                                try
                                {
                                    var scopedPoRepo = scope.ServiceProvider.GetRequiredService<IPurchaseOrderRepository>();
                                    var pdfResponse = await scopedPoRepo.GeneratePOReportPdfAsync(dto.POHeaderId);
                                    if (pdfResponse != null)
                                    {
                                        pdfBytes = pdfResponse.PdfBytes;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[CreateGRNHandler] PDF generation failed: {ex.Message}");
                                }
                            }

                            await emailService.SendGrnEmailAsync(companyNotification, supplierNotification.Email, grnNumber, poNumberLocal ?? "N/A", dto.TotalAmount, pdfBytes);
                        }

                        // 📱 WhatsApp
                        if (!string.IsNullOrEmpty(supplierNotification.Phone))
                        {
                            string msg = $"Goods Received: {grnNumber}\nRef PO: {poNumberLocal}\nSource: {companyNotification.Name}\nStatus: Received & Accepted.\nThank you!";
                            await whatsAppService.SendMessageAsync(supplierNotification.Phone, msg);
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
