using Lama.Domain.Common;

namespace Lama.Domain.MarketingManagement.Entities;

public class CampaignMetric : Entity
{
    public Guid CampaignId { get; private set; }
    public Campaign? Campaign { get; private set; }
    public MetricType MetricType { get; private set; }
    public decimal Value { get; private set; }
    public DateTime RecordedDate { get; private set; }
    public string? Notes { get; private set; }

    private CampaignMetric() { }

    private CampaignMetric(Guid campaignId, MetricType metricType, decimal value)
    {
        CampaignId = campaignId;
        MetricType = metricType;
        Value = value;
        RecordedDate = DateTime.UtcNow;
    }

    public static CampaignMetric Create(Guid campaignId, MetricType metricType, decimal value, string? notes = null)
    {
        if (campaignId == Guid.Empty)
            throw new ArgumentException("Campaign ID cannot be empty", nameof(campaignId));
        if (value < 0)
            throw new ArgumentException("Value cannot be negative", nameof(value));

        return new CampaignMetric(campaignId, metricType, value)
        {
            Notes = notes
        };
    }

    public void UpdateValue(decimal value)
    {
        if (value < 0)
            throw new ArgumentException("Value cannot be negative", nameof(value));

        Value = value;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum MetricType
{
    Impressions,
    Clicks,
    Conversions,
    Leads,
    Revenue,
    EmailsSent,
    EmailsOpened,
    LinkClicks,
    Unsubscribes,
    Registrations,
    Attendees
}
