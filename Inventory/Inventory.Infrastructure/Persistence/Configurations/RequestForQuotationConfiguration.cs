using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public class RequestForQuotationConfiguration : IEntityTypeConfiguration<RequestForQuotation>
{
    public void Configure(EntityTypeBuilder<RequestForQuotation> builder)
    {
        builder.ToTable("RequestForQuotations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RfqNo)
               .IsRequired()
               .HasMaxLength(50);

        builder.HasIndex(x => new { x.CompanyId, x.RfqNo })
               .HasDatabaseName("IX_RequestForQuotations_CompanyId_RfqNo")
               .IsUnique();

        builder.Property(x => x.SupplierId)
               .IsRequired();

        builder.Property(x => x.SupplierName)
               .HasMaxLength(150);

        builder.Property(x => x.CreatedDate)
               .IsRequired();

        builder.Property(x => x.Status)
               .IsRequired();

        builder.Property(x => x.Remarks)
               .HasMaxLength(500);

        builder.Property(x => x.BranchId)
               .HasMaxLength(100);

        builder.Property(x => x.CreatedBy)
               .HasMaxLength(150);

        builder.Property(x => x.ModifiedBy)
               .HasMaxLength(150);
    }
}
