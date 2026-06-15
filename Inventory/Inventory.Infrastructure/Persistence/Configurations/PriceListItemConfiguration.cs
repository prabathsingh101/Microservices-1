using Inventory.Domain.PriceLists;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public class PriceListItemConfiguration : IEntityTypeConfiguration<PriceListItem>
{
    public void Configure(EntityTypeBuilder<PriceListItem> builder)
    {
        builder.ToTable("PriceListItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Rate)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.DiscountPercent)
            .HasPrecision(18, 2);

        builder.Property(x => x.Discount)
            .HasPrecision(18, 2);

        builder.Property(x => x.MinQty)
            .IsRequired();

        builder.Property(x => x.MaxQty);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
