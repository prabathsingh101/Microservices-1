using Inventory.Application.Clients;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

public class UpdatePOStatusHandler : IRequestHandler<UpdatePOStatusCommand, bool>
{
    private readonly IPurchaseOrderRepository _repository;
    private readonly IServiceScopeFactory _scopeFactory;

    public UpdatePOStatusHandler(IPurchaseOrderRepository repository, IServiceScopeFactory scopeFactory)
    {
        _repository = repository;
        _scopeFactory = scopeFactory;
    }

    public async Task<bool> Handle(UpdatePOStatusCommand request, CancellationToken cancellationToken)
    {
        var result = await _repository.UpdatePOStatusAsync(request.Id, request.Status);

        if (result && (request.Status == "Approved" || request.Status == "Cancelled" || request.Status == "Received" || request.Status == "Rejected"))
        {
            // FETCH DATA BEFORE Task.Run SO HTTP CONTEXT IS AVAILABLE FOR TOKENS
            var companyClient = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ICompanyClient>();
            var supplierClient = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ISupplierClient>();
            
            Inventory.Application.Clients.DTOs.CompanyProfileDto? company = null;
            Inventory.Application.PurchaseReturn.SupplierSelectDto? supplier = null;
            PurchaseOrder po = await _repository.GetByIdAsync(request.Id);

            if (po != null)
            {
                try 
                {
                    company = await companyClient.GetCompanyProfileAsync();
                    supplier = await supplierClient.GetSupplierByIdAsync(po.SupplierId);
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"[UpdatePOStatusHandler] Failed to fetch data for notification: {ex.Message}");
                }
            }

            // Background Task to send notifications when approved
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

                try
                {
                    if (po == null) return;

                    if (company != null && supplier != null)
                    {
                        // 1. Email (Only for Approved status)
                        if (request.Status == "Approved" && !string.IsNullOrEmpty(supplier.Email))
                        {
                            byte[] pdfBytes = null;
                            try
                            {
                                var scopedRepo = scope.ServiceProvider.GetRequiredService<IPurchaseOrderRepository>();
                                var pdfResponse = await scopedRepo.GeneratePOReportPdfAsync(po.Id);
                                if (pdfResponse != null)
                                {
                                    pdfBytes = pdfResponse.PdfBytes;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[UpdatePOStatusHandler] PDF generation failed: {ex.Message}");
                            }

                            await emailService.SendPoEmailAsync(company, supplier.Email, po.PoNumber, po.GrandTotal, pdfBytes);
                        }

                        // 2. WhatsApp
                        if (!string.IsNullOrEmpty(supplier.Phone))
                        {
                            string msg = "";
                            if (request.Status == "Approved")
                            {
                                string template = company.PurchaseOrderCreationMessage;
                                if (string.IsNullOrEmpty(template))
                                {
                                    template = "Hi [SupplierName], PO #[PONo] of [Amount] is created by [CompanyName]. Please confirm delivery. Thanks!";
                                }
                                msg = template
                                    .Replace("[SupplierName]", supplier.Name, StringComparison.OrdinalIgnoreCase)
                                    .Replace("[Supplier Name]", supplier.Name, StringComparison.OrdinalIgnoreCase)
                                    .Replace("[PONo]", po.PoNumber, StringComparison.OrdinalIgnoreCase)
                                    .Replace("[PO No]", po.PoNumber, StringComparison.OrdinalIgnoreCase)
                                    .Replace("[CompanyName]", company.Name, StringComparison.OrdinalIgnoreCase)
                                    .Replace("[Company Name]", company.Name, StringComparison.OrdinalIgnoreCase)
                                    .Replace("[Amount]", "₹" + po.GrandTotal.ToString("N0"), StringComparison.OrdinalIgnoreCase)
                                    .Replace("[GrandTotal]", "₹" + po.GrandTotal.ToString("N0"), StringComparison.OrdinalIgnoreCase);
                            }
                            else
                            {
                                string template = company.PurchaseOrderStatusUpdateMessage;
                                if (string.IsNullOrEmpty(template))
                                {
                                    template = "Hi [SupplierName], PO #[PONo] from [CompanyName] is now [Status]. Expected delivery: [DeliveryDate]. Thanks!";
                                }
                                msg = template
                                    .Replace("[SupplierName]", supplier.Name, StringComparison.OrdinalIgnoreCase)
                                    .Replace("[Supplier Name]", supplier.Name, StringComparison.OrdinalIgnoreCase)
                                    .Replace("[PONo]", po.PoNumber, StringComparison.OrdinalIgnoreCase)
                                    .Replace("[PO No]", po.PoNumber, StringComparison.OrdinalIgnoreCase)
                                    .Replace("[CompanyName]", company.Name, StringComparison.OrdinalIgnoreCase)
                                    .Replace("[Company Name]", company.Name, StringComparison.OrdinalIgnoreCase)
                                    .Replace("[Status]", request.Status, StringComparison.OrdinalIgnoreCase)
                                    .Replace("[Amount]", "₹" + po.GrandTotal.ToString("N0"), StringComparison.OrdinalIgnoreCase)
                                    .Replace("[GrandTotal]", "₹" + po.GrandTotal.ToString("N0"), StringComparison.OrdinalIgnoreCase)
                                    .Replace("[DeliveryDate]", po.ExpectedDeliveryDate?.ToString("dd MMM yyyy") ?? "N/A", StringComparison.OrdinalIgnoreCase)
                                    .Replace("[Delivery Date]", po.ExpectedDeliveryDate?.ToString("dd MMM yyyy") ?? "N/A", StringComparison.OrdinalIgnoreCase)
                                    .Replace("[ExpectedDeliveryDate]", po.ExpectedDeliveryDate?.ToString("dd MMM yyyy") ?? "N/A", StringComparison.OrdinalIgnoreCase);
                            }

                            await whatsAppService.SendMessageAsync(supplier.Phone, msg);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UpdatePOStatusHandler] Notification task failed: {ex.Message}");
                }
            });
        }

        return result;
    }
}
