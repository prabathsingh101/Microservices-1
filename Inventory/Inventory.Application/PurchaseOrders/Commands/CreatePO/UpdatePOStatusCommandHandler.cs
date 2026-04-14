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
            // Background Task to send notifications when approved
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                var companyClient = scope.ServiceProvider.GetRequiredService<ICompanyClient>();
                var supplierClient = scope.ServiceProvider.GetRequiredService<ISupplierClient>();

                try
                {
                    var po = await _repository.GetByIdAsync(request.Id);
                    if (po == null) return;

                    var company = await companyClient.GetCompanyProfileAsync();
                    var supplier = await supplierClient.GetSupplierByIdAsync(po.SupplierId);

                    if (company != null && supplier != null)
                    {
                        // 1. Email
                        if (!string.IsNullOrEmpty(supplier.Email))
                        {
                            await emailService.SendPoEmailAsync(company, supplier.Email, po.PoNumber, po.GrandTotal);
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
