using Lama.Domain.Common;
using Lama.Domain.SalesManagement.ValueObjects;

namespace Lama.Domain.SalesManagement.Entities;

public class Opportunity : AggregateRoot
{
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid? ContactId { get; private set; }
    public Money ExpectedRevenue { get; private set; }
    public int Probability { get; private set; }
    public DateTime ExpectedCloseDate { get; private set; }
    public OpportunityStage Stage { get; private set; }
    public Guid? PipelineId { get; private set; }
    public SalesPipeline? Pipeline { get; private set; }

    private readonly List<SalesActivity> _activities = new();
    public IReadOnlyCollection<SalesActivity> Activities => _activities.AsReadOnly();

    private Opportunity() { }

    private Opportunity(string name, Guid accountId, Money expectedRevenue, DateTime expectedCloseDate)
    {
        Name = name;
        AccountId = accountId;
        ExpectedRevenue = expectedRevenue;
        ExpectedCloseDate = expectedCloseDate;
        Stage = OpportunityStage.Prospecting;
        Probability = 10;
    }

    public static Opportunity Create(string name, Guid accountId, decimal expectedRevenue, DateTime expectedCloseDate, string currency = "USD")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Opportunity name cannot be empty", nameof(name));
        if (accountId == Guid.Empty)
            throw new ArgumentException("Account ID cannot be empty", nameof(accountId));
        if (expectedCloseDate < DateTime.UtcNow)
            throw new ArgumentException("Expected close date must be in the future", nameof(expectedCloseDate));

        return new Opportunity(name, accountId, Money.Create(expectedRevenue, currency), expectedCloseDate);
    }

    public void UpdateOpportunityInfo(string name, string? description, decimal expectedRevenue, DateTime expectedCloseDate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Opportunity name cannot be empty", nameof(name));

        Name = name;
        Description = description;
        ExpectedRevenue = Money.Create(expectedRevenue, ExpectedRevenue.Currency);
        ExpectedCloseDate = expectedCloseDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MoveToStage(OpportunityStage newStage)
    {
        if (newStage < Stage)
            throw new InvalidOperationException("Cannot move opportunity backwards in the pipeline");

        Stage = newStage;
        Probability = GetProbabilityForStage(newStage);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Win()
    {
        Stage = OpportunityStage.Won;
        Probability = 100;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Lose(string reason)
    {
        Stage = OpportunityStage.Lost;
        Probability = 0;
        Description = $"{Description}\n\nLost Reason: {reason}";
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddActivity(SalesActivity activity)
    {
        _activities.Add(activity);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignToPipeline(Guid pipelineId)
    {
        PipelineId = pipelineId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignContact(Guid contactId)
    {
        ContactId = contactId;
        UpdatedAt = DateTime.UtcNow;
    }

    private static int GetProbabilityForStage(OpportunityStage stage)
    {
        return stage switch
        {
            OpportunityStage.Prospecting => 10,
            OpportunityStage.Qualification => 25,
            OpportunityStage.NeedsAnalysis => 40,
            OpportunityStage.Proposal => 60,
            OpportunityStage.Negotiation => 80,
            OpportunityStage.Won => 100,
            OpportunityStage.Lost => 0,
            _ => 10
        };
    }
}

public enum OpportunityStage
{
    Prospecting,
    Qualification,
    NeedsAnalysis,
    Proposal,
    Negotiation,
    Won,
    Lost
}
