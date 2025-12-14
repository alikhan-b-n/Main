using Lama.Domain.Common;

namespace Lama.Domain.MarketingManagement.Entities;

public class CustomerSegment : AggregateRoot
{
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public SegmentCriteria Criteria { get; private set; }
    public int EstimatedSize { get; private set; }

    private readonly List<Campaign> _campaigns = new();
    public IReadOnlyCollection<Campaign> Campaigns => _campaigns.AsReadOnly();

    private CustomerSegment() { }

    private CustomerSegment(string name, SegmentCriteria criteria)
    {
        Name = name;
        Criteria = criteria;
        EstimatedSize = 0;
    }

    public static CustomerSegment Create(string name, SegmentCriteria criteria, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Segment name cannot be empty", nameof(name));
        if (criteria == null)
            throw new ArgumentNullException(nameof(criteria));

        return new CustomerSegment(name, criteria)
        {
            Description = description
        };
    }

    public void UpdateSegmentInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Segment name cannot be empty", nameof(name));

        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCriteria(SegmentCriteria criteria)
    {
        if (criteria == null)
            throw new ArgumentNullException(nameof(criteria));

        Criteria = criteria;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateEstimatedSize(int size)
    {
        if (size < 0)
            throw new ArgumentException("Size cannot be negative", nameof(size));

        EstimatedSize = size;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class SegmentCriteria
{
    public string? Industry { get; set; }
    public string? Location { get; set; }
    public decimal? MinRevenue { get; set; }
    public decimal? MaxRevenue { get; set; }
    public int? MinEmployees { get; set; }
    public int? MaxEmployees { get; set; }
    public List<string> Tags { get; set; } = new();

    public bool Matches(Dictionary<string, object> customerData)
    {
        // Implementation would check if customer data matches the criteria
        return true;
    }
}
