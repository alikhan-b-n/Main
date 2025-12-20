using Lama.Domain.MarketingManagement.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class CustomerSegmentConfiguration : IEntityTypeConfiguration<CustomerSegment>
{
    public void Configure(EntityTypeBuilder<CustomerSegment> builder)
    {
        builder.HasKey(cs => cs.Id);

        builder.Property(cs => cs.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(cs => cs.Description)
            .HasMaxLength(1000);

        builder.Property(cs => cs.EstimatedSize)
            .IsRequired();

        // Ignore collections and owned types
        builder.Ignore(cs => cs.Criteria);
        builder.Ignore(cs => cs.Campaigns);
    }
}