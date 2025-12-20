using Lama.Domain.SalesManagement.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class OpportunityConfiguration : IEntityTypeConfiguration<Opportunity>
{
    public void Configure(EntityTypeBuilder<Opportunity> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.Description)
            .HasMaxLength(2000);

        builder.Property(o => o.AccountId)
            .IsRequired();

        builder.Property(o => o.Probability)
            .IsRequired();

        builder.Property(o => o.Stage)
            .IsRequired();

        builder.Property(o => o.ExpectedCloseDate)
            .IsRequired();

        // Configure relationship with Pipeline
        builder.HasOne(o => o.Pipeline)
            .WithMany()
            .HasForeignKey(o => o.PipelineId)
            .OnDelete(DeleteBehavior.SetNull);

        // Ignore the Activities collection - managed separately
        builder.Ignore(o => o.Activities);
    }
}