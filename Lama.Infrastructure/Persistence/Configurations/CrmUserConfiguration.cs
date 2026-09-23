using Lama.Domain.AccessControl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class CrmUserConfiguration : IEntityTypeConfiguration<CrmUser>
{
    public void Configure(EntityTypeBuilder<CrmUser> builder)
    {
        builder.ToTable("CrmUsers");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
        builder.Property(u => u.Role).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.Property(u => u.IsActive).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt);
        builder.Property(u => u.LastLoginAt);

        builder.Ignore(u => u.IsAdmin);
    }
}
