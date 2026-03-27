using Identity.Domain.PrintSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class RolePrintSettingConfiguration : IEntityTypeConfiguration<RolePrintSetting>
{
    public void Configure(EntityTypeBuilder<RolePrintSetting> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PageName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.PrintFormat)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
