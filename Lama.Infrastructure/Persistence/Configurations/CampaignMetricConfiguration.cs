using Lama.Domain.MarketingManagement.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class CampaignMetricConfiguration : IEntityTypeConfiguration<CampaignMetric>
{
    public void Configure(EntityTypeBuilder<CampaignMetric> builder)
    {
        builder.HasKey(cm => cm.Id);

        builder.Property(cm => cm.MetricType)
            .IsRequired();

        builder.Property(cm => cm.Value)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(cm => cm.CampaignId)
            .IsRequired();

        builder.Property(cm => cm.RecordedDate)
            .IsRequired();

        builder.Property(cm => cm.Notes)
            .HasMaxLength(500);

        // Configure relationship with Campaign
        builder.HasOne(cm => cm.Campaign)
            .WithMany()
            .HasForeignKey(cm => cm.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}