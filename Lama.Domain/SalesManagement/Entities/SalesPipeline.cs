using Lama.Domain.Common;

namespace Lama.Domain.SalesManagement.Entities;

public class SalesPipeline : AggregateRoot
{
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<Opportunity> _opportunities = new();
    public IReadOnlyCollection<Opportunity> Opportunities => _opportunities.AsReadOnly();

    private SalesPipeline() { }

    private SalesPipeline(string name)
    {
        Name = name;
        IsActive = true;
    }

    public static SalesPipeline Create(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Pipeline name cannot be empty", nameof(name));

        return new SalesPipeline(name)
        {
            Description = description
        };
    }

    public void UpdatePipelineInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Pipeline name cannot be empty", nameof(name));

        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddOpportunity(Opportunity opportunity)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot add opportunities to inactive pipeline");

        if (_opportunities.Any(o => o.Id == opportunity.Id))
            throw new InvalidOperationException("Opportunity already exists in this pipeline");

        _opportunities.Add(opportunity);
        UpdatedAt = DateTime.UtcNow;
    }

    public decimal GetTotalValue()
    {
        return _opportunities
            .Where(o => o.Stage != OpportunityStage.Lost)
            .Sum(o => o.ExpectedRevenue.Amount);
    }

    public decimal GetWeightedValue()
    {
        return _opportunities
            .Where(o => o.Stage != OpportunityStage.Lost)
            .Sum(o => o.ExpectedRevenue.Amount * o.Probability / 100m);
    }
}
