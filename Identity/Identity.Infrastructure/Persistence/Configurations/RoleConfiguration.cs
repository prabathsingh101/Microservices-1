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

        builder.HasIndex(x => new { x.RoleName, x.CompanyId })
            .IsUnique();

        // ✅ SEED DATA with Fixed GUIDs for System Roles
        builder.HasData(
            new Role("Default Admin") { Id = Guid.Parse("00000000-0000-0000-0000-000000000001") }
            //new Role("User") { Id = Guid.Parse("00000000-0000-0000-0000-000000000002") },
            //new Role("Employee") { Id = Guid.Parse("00000000-0000-0000-0000-000000000003") },
            //new Role("Warehouse") { Id = Guid.Parse("00000000-0000-0000-0000-000000000004") },
            //new Role("Super Admin") { Id = Guid.Parse("00000000-0000-0000-0000-000000000005") },
            //new Role("Manager") { Id = Guid.Parse("00000000-0000-0000-0000-000000000006") },
            //new Role("Customer") { Id = Guid.Parse("00000000-0000-0000-0000-000000000007") }
        );
    }
}
