using Lama.Domain.Common;

namespace Lama.Domain.MarketingManagement.Entities;

public class MarketingAnalytics : AggregateRoot
{
    public string Name { get; private set; }
    public AnalyticsType Type { get; private set; }
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public Guid? CampaignId { get; private set; }
    public Campaign? Campaign { get; private set; }

    private readonly Dictionary<string, decimal> _metrics = new();
    public IReadOnlyDictionary<string, decimal> Metrics => _metrics;

    private readonly List<AnalyticsInsight> _insights = new();
    public IReadOnlyCollection<AnalyticsInsight> Insights => _insights.AsReadOnly();

    private MarketingAnalytics() { }

    private MarketingAnalytics(string name, AnalyticsType type, DateTime periodStart, DateTime periodEnd)
    {
        Name = name;
        Type = type;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
    }

    public static MarketingAnalytics Create(string name, AnalyticsType type, DateTime periodStart, DateTime periodEnd, Guid? campaignId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Analytics name cannot be empty", nameof(name));
        if (periodStart >= periodEnd)
            throw new ArgumentException("Period start must be before period end");

        return new MarketingAnalytics(name, type, periodStart, periodEnd)
        {
            CampaignId = campaignId
        };
    }

    public void AddMetric(string metricName, decimal value)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentException("Metric name cannot be empty", nameof(metricName));

        _metrics[metricName] = value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddInsight(AnalyticsInsight insight)
    {
        _insights.Add(insight);
        UpdatedAt = DateTime.UtcNow;
    }

    public decimal GetConversionRate()
    {
        if (_metrics.TryGetValue("Leads", out var leads) && leads > 0 &&
            _metrics.TryGetValue("Conversions", out var conversions))
        {
            return (conversions / leads) * 100;
        }
        return 0;
    }

    public decimal GetClickThroughRate()
    {
        if (_metrics.TryGetValue("Impressions", out var impressions) && impressions > 0 &&
            _metrics.TryGetValue("Clicks", out var clicks))
        {
            return (clicks / impressions) * 100;
        }
        return 0;
    }

    public decimal GetEngagementRate()
    {
        if (_metrics.TryGetValue("TotalReach", out var reach) && reach > 0 &&
            _metrics.TryGetValue("Engagements", out var engagements))
        {
            return (engagements / reach) * 100;
        }
        return 0;
    }

    public decimal GetCostPerLead()
    {
        if (_metrics.TryGetValue("Leads", out var leads) && leads > 0 &&
            _metrics.TryGetValue("TotalCost", out var cost))
        {
            return cost / leads;
        }
        return 0;
    }

    public decimal GetCostPerAcquisition()
    {
        if (_metrics.TryGetValue("Conversions", out var conversions) && conversions > 0 &&
            _metrics.TryGetValue("TotalCost", out var cost))
        {
            return cost / conversions;
        }
        return 0;
    }
}

public class AnalyticsInsight
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public InsightSeverity Severity { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<string> Recommendations { get; set; } = new();
}

public enum AnalyticsType
{
    Campaign,
    Channel,
    Segment,
    Overall,
    Attribution,
    Funnel
}

public enum InsightSeverity
{
    Info,
    Warning,
    Critical,
    Opportunity
}
