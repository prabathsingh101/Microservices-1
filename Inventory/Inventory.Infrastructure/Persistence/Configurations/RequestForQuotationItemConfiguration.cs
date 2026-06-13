using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public class RequestForQuotationItemConfiguration : IEntityTypeConfiguration<RequestForQuotationItem>
{
    public void Configure(EntityTypeBuilder<RequestForQuotationItem> builder)
    {
        builder.ToTable("RequestForQuotationItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RfqId)
               .IsRequired();

        builder.Property(x => x.ProductId)
               .IsRequired();

        builder.Property(x => x.Qty)
               .HasPrecision(18, 2)
               .IsRequired();

        builder.Property(x => x.UnitPrice)
               .HasPrecision(18, 2);

        builder.Property(x => x.TaxRate)
               .HasPrecision(18, 2);

        builder.Property(x => x.Discount)
               .HasPrecision(18, 2);

        builder.Property(x => x.TotalCost)
               .HasPrecision(18, 2);

        builder.Property(x => x.BranchId)
               .HasMaxLength(100);

        builder.Property(x => x.CreatedBy)
               .HasMaxLength(150);

        builder.Property(x => x.ModifiedBy)
               .HasMaxLength(150);

        // Relationships
        builder.HasOne(x => x.RequestForQuotation)
               .WithMany(x => x.Items)
               .HasForeignKey(x => x.RfqId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Product)
               .WithMany()
               .HasForeignKey(x => x.ProductId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
