using DinkToPdf;
using DinkToPdf.Contracts;
using Inventory.Application.Clients;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Services;
using Inventory.Infrastructure.Clients;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<InventoryDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("InventoryDb"),
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 10,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    }));

            services.AddScoped<ICategoryRepository, CategoryRepository>();

            services.AddScoped<ISubcategoryRepository, SubcategoryRepository>();

            services.AddScoped<IProductRepository, ProductRepository>();

            services.AddScoped<IPriceListRepository, PriceListRepository>();

            services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
            services.AddScoped<IGRNRepository, GRNRepository>();
            services.AddScoped<IStockRepository, StockRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ISaleOrderRepository, SaleOrderRepository>();

            // Program.cs ke andar builder.Services mein add karein
            services.AddScoped<IPurchaseReturnRepository, PurchaseReturnRepository>();
            services.AddScoped<ICustomerClient, CustomerClient>();
            services.AddScoped<ISaleReturnRepository, SaleReturnRepository>();
            services.AddScoped<ISaleReturnService, SaleReturnService>();

            services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

            services.AddScoped<IPdfService, PdfService>();

            services.AddScoped<IDashboardRepository, DashboardRepository>();

            services.AddScoped<ICustomerHttpService, CustomerHttpService>();

            services.AddScoped<ISupplierClient, SupplierClient>();
            services.AddScoped<ICompanyClient, CompanyClient>();

            services.AddScoped<INotificationRepository, NotificationRepository>();
            
            services.AddScoped<IUnitRepository, UnitRepository>();
            services.AddScoped<IWarehouseRepository, WarehouseRepository>();
            services.AddScoped<IRackRepository, RackRepository>();
            services.AddScoped<ICurrentUserService, Inventory.Infrastructure.Services.CurrentUserService>();

            services.AddScoped<IInventoryDbContext>(
            provider => provider.GetRequiredService<InventoryDbContext>());

            return services;
        }
    }
}
