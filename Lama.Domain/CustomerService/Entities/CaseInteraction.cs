using Lama.Domain.Common;

namespace Lama.Domain.CustomerService.Entities;

public class CaseInteraction : Entity
{
    public Guid CaseId { get; private set; }
    public SupportCase? Case { get; private set; }
    public InteractionType Type { get; private set; }
    public InteractionDirection Direction { get; private set; }
    public string Subject { get; private set; }
    public string Content { get; private set; }
    public Guid? UserId { get; private set; }
    public DateTime InteractionDate { get; private set; }
    public int? DurationMinutes { get; private set; }

    private CaseInteraction() { }

    private CaseInteraction(Guid caseId, InteractionType type, InteractionDirection direction, string subject, string content)
    {
        CaseId = caseId;
        Type = type;
        Direction = direction;
        Subject = subject;
        Content = content;
        InteractionDate = DateTime.UtcNow;
    }

    public static CaseInteraction Create(Guid caseId, InteractionType type, InteractionDirection direction, string subject, string content)
    {
        if (caseId == Guid.Empty)
            throw new ArgumentException("Case ID cannot be empty", nameof(caseId));
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject cannot be empty", nameof(subject));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be empty", nameof(content));

        return new CaseInteraction(caseId, type, direction, subject, content);
    }

    public void AssignUser(Guid userId)
    {
        UserId = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDuration(int durationMinutes)
    {
        if (durationMinutes < 0)
            throw new ArgumentException("Duration cannot be negative", nameof(durationMinutes));

        DurationMinutes = durationMinutes;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum InteractionType
{
    Email,
    Phone,
    Chat,
    InPerson,
    SocialMedia,
    Note
}

public enum InteractionDirection
{
    Inbound,
    Outbound,
    Internal
}
