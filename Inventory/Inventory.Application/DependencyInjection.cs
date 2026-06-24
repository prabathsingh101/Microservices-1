using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<AssemblyReference>());

        services.AddScoped<Services.IEmailService, Services.EmailService>();
        services.AddScoped<Services.IWhatsAppService, Services.WhatsAppService>();
        services.AddScoped<Gst.Services.IGstService, Gst.Services.GstService>();

        return services;
    }
}
