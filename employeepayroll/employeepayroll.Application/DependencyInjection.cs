using Microsoft.Extensions.DependencyInjection;
using System.Reflection.Metadata;

namespace employeepayroll.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {

            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssemblyContaining<AssemblyReference>());

            return services;
        }
    }
}
