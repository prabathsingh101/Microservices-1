using Company.Application.Common.Interfaces;
using Company.Infrastructure.Persistence;
using Company.Infrastructure.Repositories;
using Company.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Company.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<CompanyDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("CompanyDb"),
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 10,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    }));

            services.AddScoped<ICompanyRepository, CompanyRepository>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddHttpClient();
            services.AddHttpContextAccessor();

            return services;
        }
    }
}