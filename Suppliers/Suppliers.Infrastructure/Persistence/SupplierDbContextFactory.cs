using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Suppliers.Infrastructure.Persistence
{
    public class SupplierDbContextFactory : IDesignTimeDbContextFactory<SupplierDbContext>
    {
        public SupplierDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Suppliers.API"))
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<SupplierDbContext>();
            var connectionString = configuration.GetConnectionString("SuppliersDb");

            optionsBuilder.UseSqlServer(connectionString);

            return new SupplierDbContext(optionsBuilder.Options, null);
        }
    }
}
