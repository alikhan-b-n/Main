using Lama.Domain.Common;

namespace Lama.Domain.SalesManagement.Entities;

public class SalesActivity : Entity
{
    public string Subject { get; private set; }
    public string? Description { get; private set; }
    public ActivityType Type { get; private set; }
    public DateTime ScheduledDate { get; private set; }
    public DateTime? CompletedDate { get; private set; }
    public ActivityStatus Status { get; private set; }
    public Guid OpportunityId { get; private set; }
    public Opportunity? Opportunity { get; private set; }
    public Guid? ContactId { get; private set; }

    private SalesActivity() { }

    private SalesActivity(string subject, ActivityType type, DateTime scheduledDate, Guid opportunityId)
    {
        Subject = subject;
        Type = type;
        ScheduledDate = scheduledDate;
        OpportunityId = opportunityId;
        Status = ActivityStatus.Scheduled;
    }

    public static SalesActivity Create(string subject, ActivityType type, DateTime scheduledDate, Guid opportunityId)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Activity subject cannot be empty", nameof(subject));
        if (opportunityId == Guid.Empty)
            throw new ArgumentException("Opportunity ID cannot be empty", nameof(opportunityId));

        return new SalesActivity(subject, type, scheduledDate, opportunityId);
    }

    public void UpdateActivityInfo(string subject, string? description, DateTime scheduledDate)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Activity subject cannot be empty", nameof(subject));

        Subject = subject;
        Description = description;
        ScheduledDate = scheduledDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status == ActivityStatus.Completed)
            throw new InvalidOperationException("Activity is already completed");

        Status = ActivityStatus.Completed;
        CompletedDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == ActivityStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed activity");

        Status = ActivityStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignContact(Guid contactId)
    {
        ContactId = contactId;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum ActivityType
{
    Call,
    Email,
    Meeting,
    Task,
    Demo,
    Presentation
}

public enum ActivityStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled
}
