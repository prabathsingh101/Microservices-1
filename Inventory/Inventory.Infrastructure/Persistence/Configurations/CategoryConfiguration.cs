using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CategoryCode)
               .IsRequired(false)
               .HasMaxLength(50);

        builder.Property(x => x.CategoryName)
               .IsRequired()
               .HasMaxLength(150);

        builder.Property(x => x.DefaultGst)
               .HasPrecision(5, 2)   // e.g. 18.00
               .IsRequired();

        builder.Property(x => x.Description)
               .HasMaxLength(500);

        builder.Property(x => x.IsActive)
               .IsRequired();

        //builder.Property(x => x.CreatedOn).IsRequired();
        //builder.Property(x => x.ModifiedOn).IsRequired();
        //builder.Property(x => x.ModifiedBy).IsRequired();
    }
}
