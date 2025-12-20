using Lama.Domain.CustomerService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class CaseInteractionConfiguration : IEntityTypeConfiguration<CaseInteraction>
{
    public void Configure(EntityTypeBuilder<CaseInteraction> builder)
    {
        builder.HasKey(ci => ci.Id);

        builder.Property(ci => ci.Content)
            .IsRequired()
            .HasMaxLength(5000);

        builder.Property(ci => ci.Type)
            .IsRequired();

        builder.Property(ci => ci.CaseId)
            .IsRequired();

        builder.Property(ci => ci.InteractionDate)
            .IsRequired();

        // Configure relationship with SupportCase
        builder.HasOne(ci => ci.Case)
            .WithMany()
            .HasForeignKey(ci => ci.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}