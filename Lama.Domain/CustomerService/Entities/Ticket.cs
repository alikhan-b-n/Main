using Lama.Domain.Common;

namespace Lama.Domain.CustomerService.Entities;

public class Ticket : AggregateRoot
{
    public string TicketName { get; private set; }
    public string Description { get; private set; }
    public TicketStatus Status { get; private set; }
    public TicketPriority Priority { get; private set; }
    public TicketSource Source { get; private set; }
    public Guid ContactId { get; private set; }
    public Guid? CompanyId { get; private set; }
    public Guid PipelineId { get; private set; }
    public Guid StageId { get; private set; }
    public Guid? OwnerId { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    private Ticket() { }

    private Ticket(string ticketName, string description, Guid contactId, TicketPriority priority, TicketSource source, Guid pipelineId, Guid stageId)
    {
        TicketName = ticketName;
        Description = description;
        ContactId = contactId;
        Priority = priority;
        Source = source;
        PipelineId = pipelineId;
        StageId = stageId;
        Status = TicketStatus.Open;
    }

    public static Ticket Create(string ticketName, string description, Guid contactId, TicketPriority priority, TicketSource source, Guid pipelineId, Guid stageId)
    {
        if (string.IsNullOrWhiteSpace(ticketName))
            throw new ArgumentException("Ticket name cannot be empty", nameof(ticketName));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Ticket description cannot be empty", nameof(description));
        if (contactId == Guid.Empty)
            throw new ArgumentException("Contact ID cannot be empty", nameof(contactId));
        if (pipelineId == Guid.Empty)
            throw new ArgumentException("Pipeline ID cannot be empty", nameof(pipelineId));
        if (stageId == Guid.Empty)
            throw new ArgumentException("Stage ID cannot be empty", nameof(stageId));

        return new Ticket(ticketName, description, contactId, priority, source, pipelineId, stageId);
    }

    public void UpdateTicketInfo(string ticketName, string description, TicketPriority priority)
    {
        if (string.IsNullOrWhiteSpace(ticketName))
            throw new ArgumentException("Ticket name cannot be empty", nameof(ticketName));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Ticket description cannot be empty", nameof(description));

        TicketName = ticketName;
        Description = description;
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignToOwner(Guid ownerId)
    {
        if (ownerId == Guid.Empty)
            throw new ArgumentException("Owner ID cannot be empty", nameof(ownerId));

        OwnerId = ownerId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MoveToStage(Guid newStageId)
    {
        StageId = newStageId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void EscalatePriority()
    {
        Priority = Priority switch
        {
            TicketPriority.Low => TicketPriority.Medium,
            TicketPriority.Medium => TicketPriority.High,
            TicketPriority.High => TicketPriority.Urgent,
            _ => Priority
        };
        UpdatedAt = DateTime.UtcNow;
    }

    public void Close()
    {
        Status = TicketStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reopen()
    {
        if (Status != TicketStatus.Closed)
            throw new InvalidOperationException("Only closed tickets can be reopened");

        Status = TicketStatus.Open;
        ClosedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignToCompany(Guid companyId)
    {
        CompanyId = companyId;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum TicketStatus
{
    Open,
    InProgress,
    Waiting,
    Closed,
    Cancelled
}

public enum TicketPriority
{
    Low,
    Medium,
    High,
    Urgent
}

public enum TicketSource
{
    Email,
    Form,
    Chat,
    Phone,
    Api,
    Other
}
