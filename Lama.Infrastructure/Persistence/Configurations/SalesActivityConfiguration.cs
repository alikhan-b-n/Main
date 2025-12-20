using Lama.Domain.SalesManagement.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class SalesActivityConfiguration : IEntityTypeConfiguration<SalesActivity>
{
    public void Configure(EntityTypeBuilder<SalesActivity> builder)
    {
        builder.HasKey(sa => sa.Id);

        builder.Property(sa => sa.Subject)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(sa => sa.Description)
            .HasMaxLength(2000);

        builder.Property(sa => sa.Type)
            .IsRequired();

        builder.Property(sa => sa.Status)
            .IsRequired();

        builder.Property(sa => sa.OpportunityId)
            .IsRequired();

        builder.Property(sa => sa.ScheduledDate)
            .IsRequired();

        // Configure relationship with Opportunity
        builder.HasOne(sa => sa.Opportunity)
            .WithMany()
            .HasForeignKey(sa => sa.OpportunityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}