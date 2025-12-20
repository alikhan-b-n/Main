using Lama.Domain.CustomerService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class SupportCaseConfiguration : IEntityTypeConfiguration<SupportCase>
{
    public void Configure(EntityTypeBuilder<SupportCase> builder)
    {
        builder.HasKey(sc => sc.Id);

        builder.Property(sc => sc.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(sc => sc.Description)
            .HasMaxLength(5000);

        builder.Property(sc => sc.Priority)
            .IsRequired();

        builder.Property(sc => sc.Status)
            .IsRequired();

        builder.Property(sc => sc.ContactId)
            .IsRequired();

        // Ignore the Interactions collection
        builder.Ignore(sc => sc.Interactions);
    }
}