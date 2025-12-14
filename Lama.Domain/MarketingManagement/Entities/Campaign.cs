using Lama.Domain.Common;

namespace Lama.Domain.MarketingManagement.Entities;

public class Campaign : AggregateRoot
{
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public CampaignType Type { get; private set; }
    public CampaignStatus Status { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public decimal Budget { get; private set; }
    public decimal ActualCost { get; private set; }

    private readonly List<CustomerSegment> _targetSegments = new();
    public IReadOnlyCollection<CustomerSegment> TargetSegments => _targetSegments.AsReadOnly();

    private readonly List<CampaignMetric> _metrics = new();
    public IReadOnlyCollection<CampaignMetric> Metrics => _metrics.AsReadOnly();

    private Campaign() { }

    private Campaign(string name, CampaignType type, DateTime startDate, decimal budget)
    {
        Name = name;
        Type = type;
        StartDate = startDate;
        Budget = budget;
        Status = CampaignStatus.Draft;
        ActualCost = 0;
    }

    public static Campaign Create(string name, CampaignType type, DateTime startDate, decimal budget)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Campaign name cannot be empty", nameof(name));
        if (budget < 0)
            throw new ArgumentException("Budget cannot be negative", nameof(budget));

        return new Campaign(name, type, startDate, budget);
    }

    public void UpdateCampaignInfo(string name, string? description, decimal budget)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Campaign name cannot be empty", nameof(name));
        if (budget < 0)
            throw new ArgumentException("Budget cannot be negative", nameof(budget));

        Name = name;
        Description = description;
        Budget = budget;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Launch()
    {
        if (Status != CampaignStatus.Draft)
            throw new InvalidOperationException("Only draft campaigns can be launched");
        if (!_targetSegments.Any())
            throw new InvalidOperationException("Campaign must have at least one target segment");

        Status = CampaignStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Pause()
    {
        if (Status != CampaignStatus.Active)
            throw new InvalidOperationException("Only active campaigns can be paused");

        Status = CampaignStatus.Paused;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Resume()
    {
        if (Status != CampaignStatus.Paused)
            throw new InvalidOperationException("Only paused campaigns can be resumed");

        Status = CampaignStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != CampaignStatus.Active && Status != CampaignStatus.Paused)
            throw new InvalidOperationException("Only active or paused campaigns can be completed");

        Status = CampaignStatus.Completed;
        EndDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddTargetSegment(CustomerSegment segment)
    {
        if (_targetSegments.Any(s => s.Id == segment.Id))
            throw new InvalidOperationException("Segment already added to this campaign");

        _targetSegments.Add(segment);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordCost(decimal cost)
    {
        if (cost < 0)
            throw new ArgumentException("Cost cannot be negative", nameof(cost));

        ActualCost += cost;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddMetric(CampaignMetric metric)
    {
        _metrics.Add(metric);
        UpdatedAt = DateTime.UtcNow;
    }

    public decimal GetROI()
    {
        if (ActualCost == 0)
            return 0;

        var revenue = _metrics
            .Where(m => m.MetricType == MetricType.Revenue)
            .Sum(m => m.Value);

        return (revenue - ActualCost) / ActualCost * 100;
    }
}

public enum CampaignType
{
    Email,
    SocialMedia,
    ContentMarketing,
    PaidAdvertising,
    Events,
    Webinar,
    DirectMail
}

public enum CampaignStatus
{
    Draft,
    Active,
    Paused,
    Completed,
    Cancelled
}
