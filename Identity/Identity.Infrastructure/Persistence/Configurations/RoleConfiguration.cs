using Identity.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RoleName)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.RoleName)
            .IsUnique();

        // ✅ SEED DATA
        builder.HasData(
            new Role(1, "Admin"),
            new Role(2, "User"),
            new Role(3, "Employee"),
            new Role(4, "Warehouse"),
            new Role(5, "Super Admin"),
            new Role(6, "Manager"),
            new Role(7, "Customer")
        );
    }
}
