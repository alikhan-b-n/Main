using Lama.Domain.CustomerManagement.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("Contacts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.JobTitle)
            .HasMaxLength(200);

        builder.Property(c => c.LifecycleStage)
            .HasMaxLength(50);

        builder.Property(c => c.TotalRevenueContribution)
            .HasPrecision(18, 2);

        builder.Property(c => c.LastActivityAt)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt);

        // Relationship with Company is configured in CompanyConfiguration

        builder.HasIndex(c => c.CompanyId);
        builder.HasIndex(c => c.OwnerId);
        builder.HasIndex(c => c.LifecycleStage);
    }
}