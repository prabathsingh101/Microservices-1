using Customers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Customers.Infrastructure.Persistence
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("Customers");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.CustomerName)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(x => x.Phone)
                   .IsRequired()
                   .HasMaxLength(20);

            builder.Property(x => x.Email)
                   .HasMaxLength(200);

            builder.Property(x => x.GstNumber)
                   .HasMaxLength(50);

            builder.Property(x => x.CreditLimit)
                   .HasColumnType("decimal(18,2)");

            builder.Property(x => x.CustomerType)
                   .IsRequired();

            builder.Property(x => x.Status)
                   .HasMaxLength(20);

            builder.Property(x => x.DrugLicenseNo)
                   .HasMaxLength(100);

            // Value Objects
            builder.OwnsOne(x => x.BillingAddress, a =>
            {
                a.Property(p => p.AddressLine)
                 .HasColumnName("BillingAddress")
                 .HasMaxLength(500)
                 .IsRequired();
            });

            builder.OwnsOne(x => x.ShippingAddress, a =>
            {
                a.Property(p => p.AddressLine)
                 .HasColumnName("ShippingAddress")
                 .HasMaxLength(500);
            });
        }
    }
}
