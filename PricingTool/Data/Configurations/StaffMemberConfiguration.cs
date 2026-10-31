using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingTool.Data.Entities;

namespace PricingTool.Data.Configurations;

public class StaffMemberConfiguration : IEntityTypeConfiguration<StaffMember>
{
    public void Configure(EntityTypeBuilder<StaffMember> builder)
    {
        builder.Property(s => s.EntraObjectId).HasMaxLength(64);
        builder.Property(s => s.DisplayName).HasMaxLength(200);
        builder.Property(s => s.Email).HasMaxLength(320);
        builder.Property(s => s.Phone).HasMaxLength(32);
        builder.Property(s => s.LastRolesSeen).HasMaxLength(64);

        builder.HasIndex(s => s.EntraObjectId).IsUnique();
    }
}
