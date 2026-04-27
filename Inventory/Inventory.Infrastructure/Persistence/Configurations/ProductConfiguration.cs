using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration
    : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Sku)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.Sku)
               .IsUnique();

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(150);

        builder.Property(x => x.Unit)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(x => x.BasePurchasePrice)
               .HasPrecision(18, 2);

        builder.Property(x => x.MRP)
               .HasPrecision(18, 2);

        builder.Property(x => x.SaleRate)
               .HasPrecision(18, 2);

        builder.Property(x => x.DefaultGst)
               .HasPrecision(5, 2)
               .IsRequired(false);

        builder.Property(x => x.DamagedStock)
               .HasPrecision(18, 2);

        builder.Property(x => x.Description)
               .HasMaxLength(500);

        builder.Property(x => x.TrackInventory)
               .IsRequired();

        builder.Property(x => x.CategoryId)
               .IsRequired();

        builder.Property(x => x.SubcategoryId)
               .IsRequired();

        // FK ? Category
        builder.HasOne(p => p.Category)
               .WithMany()
               .HasForeignKey(p => p.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);

        // FK ? Subcategory
        builder.HasOne(p => p.Subcategory)
               .WithMany()
               .HasForeignKey(p => p.SubcategoryId)
               .OnDelete(DeleteBehavior.Restrict);

        // FK ? Default Warehouse/Rack
        builder.HasOne(p => p.DefaultWarehouse)
               .WithMany()
               .HasForeignKey(p => p.DefaultWarehouseId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(p => p.DefaultRack)
               .WithMany()
               .HasForeignKey(p => p.DefaultRackId)
               .OnDelete(DeleteBehavior.NoAction);
    }
}
