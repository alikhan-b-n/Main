using Lama.Domain.Common;

namespace Lama.Domain.CustomerService.Entities;

public class SupportCase : AggregateRoot
{
    public string Title { get; private set; }
    public string Description { get; private set; }
    public CaseStatus Status { get; private set; }
    public CasePriority Priority { get; private set; }
    public CaseType Type { get; private set; }
    public Guid ContactId { get; private set; }
    public Guid? AccountId { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public DateTime? ResolvedDate { get; private set; }
    public DateTime? ClosedDate { get; private set; }
    public string? Resolution { get; private set; }

    private readonly List<CaseInteraction> _interactions = new();
    public IReadOnlyCollection<CaseInteraction> Interactions => _interactions.AsReadOnly();

    private SupportCase() { }

    private SupportCase(string title, string description, Guid contactId, CasePriority priority, CaseType type)
    {
        Title = title;
        Description = description;
        ContactId = contactId;
        Priority = priority;
        Type = type;
        Status = CaseStatus.New;
    }

    public static SupportCase Create(string title, string description, Guid contactId, CasePriority priority, CaseType type)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Case title cannot be empty", nameof(title));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Case description cannot be empty", nameof(description));
        if (contactId == Guid.Empty)
            throw new ArgumentException("Contact ID cannot be empty", nameof(contactId));

        return new SupportCase(title, description, contactId, priority, type);
    }

    public void UpdateCaseInfo(string title, string description, CasePriority priority)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Case title cannot be empty", nameof(title));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Case description cannot be empty", nameof(description));

        Title = title;
        Description = description;
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignTo(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        AssignedToUserId = userId;
        if (Status == CaseStatus.New)
            Status = CaseStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void EscalatePriority()
    {
        Priority = Priority switch
        {
            CasePriority.Low => CasePriority.Medium,
            CasePriority.Medium => CasePriority.High,
            CasePriority.High => CasePriority.Critical,
            _ => Priority
        };
        UpdatedAt = DateTime.UtcNow;
    }

    public void Resolve(string resolution)
    {
        if (string.IsNullOrWhiteSpace(resolution))
            throw new ArgumentException("Resolution cannot be empty", nameof(resolution));

        Status = CaseStatus.Resolved;
        Resolution = resolution;
        ResolvedDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Close()
    {
        if (Status != CaseStatus.Resolved)
            throw new InvalidOperationException("Only resolved cases can be closed");

        Status = CaseStatus.Closed;
        ClosedDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reopen(string reason)
    {
        if (Status != CaseStatus.Closed)
            throw new InvalidOperationException("Only closed cases can be reopened");

        Status = CaseStatus.InProgress;
        Description += $"\n\nReopened: {reason}";
        ResolvedDate = null;
        ClosedDate = null;
        Resolution = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddInteraction(CaseInteraction interaction)
    {
        _interactions.Add(interaction);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignAccount(Guid accountId)
    {
        AccountId = accountId;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum CaseStatus
{
    New,
    InProgress,
    Waiting,
    Resolved,
    Closed,
    Cancelled
}

public enum CasePriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum CaseType
{
    Question,
    Problem,
    FeatureRequest,
    Bug,
    Complaint,
    Other
}
