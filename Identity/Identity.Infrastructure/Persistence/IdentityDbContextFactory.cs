using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
namespace Identity.Infrastructure.Persistence;

public class IdentityDbContextFactory
    : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();

        optionsBuilder.UseSqlServer(
            configuration.GetConnectionString("IdentityDb")
            ?? "Server=localhost,1433;Database=IdentityDb;user id=sa;password=Anand@raj12345;TrustServerCertificate=True");

        return new IdentityDbContext(optionsBuilder.Options, new DesignTimeCurrentUserService());
    }

    private class DesignTimeCurrentUserService : Application.Interfaces.ICurrentUserService
    {
        public Guid? CompanyId => null;
        public string? BranchId => null;
        public Guid? UserId => null;
        public bool IsSuperAdmin => false;
    }
}
