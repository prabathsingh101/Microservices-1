using employeepayroll.Application.Common.Interfaces;
using employeepayroll.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace employeepayroll.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<EmployeePayrollDBContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("EmployeePayrollDb"),
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 10,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    }));

            services.AddScoped<IEmployeePayrollDbContext>(provider => provider.GetRequiredService<EmployeePayrollDBContext>());

            return services;
        }
    }
}
