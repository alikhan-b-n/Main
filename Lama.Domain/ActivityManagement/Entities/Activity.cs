using Lama.Domain.Common;

namespace Lama.Domain.ActivityManagement.Entities;

public class Activity : AggregateRoot
{
    public ActivityType Type { get; private set; }
    public string Subject { get; private set; }
    public string? Body { get; private set; }
    public DateTime? DueDate { get; private set; }
    public bool IsCompleted { get; private set; }
    public Guid? OwnerId { get; private set; }
    public DateTime Timestamp { get; private set; }

    // Associations
    public Guid? ContactId { get; private set; }
    public Guid? CompanyId { get; private set; }
    public Guid? DealId { get; private set; }
    public Guid? TicketId { get; private set; }

    // AI-generated summary and metadata
    public string? Summary { get; private set; }
    public string? AiMetadata { get; private set; }

    private Activity() { }

    private Activity(ActivityType type, string subject, Guid? ownerId)
    {
        Type = type;
        Subject = subject;
        OwnerId = ownerId;
        Timestamp = DateTime.UtcNow;
        IsCompleted = false;
    }

    public static Activity Create(ActivityType type, string subject, Guid? ownerId = null, string? body = null, DateTime? dueDate = null)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Activity subject cannot be empty", nameof(subject));

        return new Activity(type, subject, ownerId)
        {
            Body = body,
            DueDate = dueDate
        };
    }

    public void UpdateActivityInfo(string subject, string? body, DateTime? dueDate)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Activity subject cannot be empty", nameof(subject));

        Subject = subject;
        Body = body;
        DueDate = dueDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        IsCompleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reopen()
    {
        IsCompleted = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignToOwner(Guid ownerId)
    {
        OwnerId = ownerId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssociateWithContact(Guid contactId)
    {
        ContactId = contactId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssociateWithCompany(Guid companyId)
    {
        CompanyId = companyId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssociateWithDeal(Guid dealId)
    {
        DealId = dealId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssociateWithTicket(Guid ticketId)
    {
        TicketId = ticketId;
        UpdatedAt = DateTime.UtcNow;
    }

    // Set or update AI-generated summary and metadata
    public void UpdateSummary(string summary, string? aiMetadata = null)
    {
        Summary = summary;
        AiMetadata = aiMetadata;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum ActivityType
{
    Task,
    Call,
    Meeting,
    Note,
    Email
}
