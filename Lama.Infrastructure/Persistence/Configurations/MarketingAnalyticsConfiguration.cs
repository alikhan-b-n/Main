using Lama.Domain.MarketingManagement.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class MarketingAnalyticsConfiguration : IEntityTypeConfiguration<MarketingAnalytics>
{
    public void Configure(EntityTypeBuilder<MarketingAnalytics> builder)
    {
        builder.HasKey(ma => ma.Id);

        builder.Property(ma => ma.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ma => ma.Type)
            .IsRequired();

        builder.Property(ma => ma.PeriodStart)
            .IsRequired();

        builder.Property(ma => ma.PeriodEnd)
            .IsRequired();

        // Configure relationship with Campaign
        builder.HasOne(ma => ma.Campaign)
            .WithMany()
            .HasForeignKey(ma => ma.CampaignId)
            .OnDelete(DeleteBehavior.SetNull);

        // Ignore collections
        builder.Ignore(ma => ma.Metrics);
        builder.Ignore(ma => ma.Insights);
    }
}