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

        if (result && request.Status == "Approved")
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
                        // 1. Email
                        if (!string.IsNullOrEmpty(supplier.Email))
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
                            string msg = $"New Purchase Order from {company.Name}:\nPO Number: {po.PoNumber}\nAmount: {po.GrandTotal}\nPlease check your email for details.";
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
