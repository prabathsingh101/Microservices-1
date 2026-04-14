using Identity.Application.Interfaces;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Repositories;
using Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("IdentityDb"),
                sqlServerOptionsAction: sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 10,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                }));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IRefreshTokenRepository,  RefreshTokenRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<IRolePrintSettingRepository, RolePrintSettingRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();

        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, Services.CurrentUserService>();
        services.AddScoped<IOnboardingService, Services.OnboardingService>();
        return services;
    }
}
