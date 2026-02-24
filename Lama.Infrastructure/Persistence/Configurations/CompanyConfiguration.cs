using Lama.Domain.CustomerManagement.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Domain)
            .HasMaxLength(255);

        builder.Property(c => c.Industry)
            .HasMaxLength(100);

        builder.Property(c => c.Website)
            .HasMaxLength(255);

        builder.Property(c => c.TotalSpent)
            .HasPrecision(18, 2);

        builder.Property(c => c.LastActivityAt)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt);

        builder.HasMany(c => c.Contacts)
            .WithOne(c => c.Company)
            .HasForeignKey(c => c.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.Name);
        builder.HasIndex(c => c.Domain);
        builder.HasIndex(c => c.ClientCategoryId);
    }
}
