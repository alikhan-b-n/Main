using Lama.Domain.Common;
using Lama.Domain.SalesManagement.ValueObjects;

namespace Lama.Domain.SalesManagement.Entities;

public class Deal : AggregateRoot
{
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid? ContactId { get; private set; }
    public Money Amount { get; private set; }
    public int Probability { get; private set; }
    public DateTime ExpectedCloseDate { get; private set; }
    public DateTime? ActualCloseDate { get; private set; }
    public Guid PipelineId { get; private set; }
    public Guid StageId { get; private set; }
    public Guid? OwnerId { get; private set; }

    private Deal() { }

    private Deal(string name, Guid companyId, Money amount, DateTime expectedCloseDate, Guid pipelineId, Guid stageId)
    {
        Name = name;
        CompanyId = companyId;
        Amount = amount;
        ExpectedCloseDate = expectedCloseDate;
        PipelineId = pipelineId;
        StageId = stageId;
        Probability = 10; // Default probability
    }

    public static Deal Create(string name, Guid companyId, decimal amount, DateTime expectedCloseDate, Guid pipelineId, Guid stageId, string currency = "USD")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Deal name cannot be empty", nameof(name));
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID cannot be empty", nameof(companyId));
        if (pipelineId == Guid.Empty)
            throw new ArgumentException("Pipeline ID cannot be empty", nameof(pipelineId));
        if (stageId == Guid.Empty)
            throw new ArgumentException("Stage ID cannot be empty", nameof(stageId));
        if (expectedCloseDate < DateTime.UtcNow)
            throw new ArgumentException("Expected close date must be in the future", nameof(expectedCloseDate));

        return new Deal(name, companyId, Money.Create(amount, currency), expectedCloseDate, pipelineId, stageId);
    }

    public void UpdateDealInfo(string name, string? description, decimal amount, DateTime expectedCloseDate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Deal name cannot be empty", nameof(name));

        Name = name;
        Description = description;
        Amount = Money.Create(amount, Amount.Currency);
        ExpectedCloseDate = expectedCloseDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MoveToStage(Guid newStageId, int stageProbability)
    {
        StageId = newStageId;
        Probability = stageProbability;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Win()
    {
        Probability = 100;
        ActualCloseDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Lose(string reason)
    {
        Probability = 0;
        Description = $"{Description}\n\nLost Reason: {reason}";
        ActualCloseDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignToPipeline(Guid pipelineId, Guid initialStageId)
    {
        PipelineId = pipelineId;
        StageId = initialStageId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignContact(Guid contactId)
    {
        ContactId = contactId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignOwner(Guid ownerId)
    {
        OwnerId = ownerId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProbability(int probability)
    {
        if (probability < 0 || probability > 100)
            throw new ArgumentException("Probability must be between 0 and 100", nameof(probability));

        Probability = probability;
        UpdatedAt = DateTime.UtcNow;
    }
}
