using Identity.Domain.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        builder.HasKey(rp => rp.Id);

        // Configure foreign keys
        builder.Property(rp => rp.RoleId)
            .IsRequired(false);

        builder.Property(rp => rp.MenuId)
            .IsRequired();

        // Configure navigation properties as OPTIONAL
        // This prevents EF Core from requiring them during model validation
        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne(rp => rp.Menu)
            .WithMany()
            .HasForeignKey(rp => rp.MenuId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false); // Navigation property is optional

        // Configure permission flags
        builder.Property(rp => rp.CanView)
            .IsRequired();

        builder.Property(rp => rp.CanAdd)
            .IsRequired();

        builder.Property(rp => rp.CanEdit)
            .IsRequired();

        builder.Property(rp => rp.CanDelete)
            .IsRequired();

        builder.Property(rp => rp.UserId)
            .IsRequired(false);
    }
}
