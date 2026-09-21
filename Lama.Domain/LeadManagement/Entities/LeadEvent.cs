using Lama.Domain.Common;

namespace Lama.Domain.LeadManagement.Entities;

/// <summary>
/// Timeline entry of a lead. Stores codes rather than prose (status names, kinds)
/// so the UI can render it in either language.
/// </summary>
public class LeadEvent : Entity
{
    public const int MaxTextLength = 2000;

    public Guid LeadId { get; private set; }
    public LeadEventKind Kind { get; private set; }
    public LeadStatus? FromStatus { get; private set; }
    public LeadStatus? ToStatus { get; private set; }
    public string? Text { get; private set; }

    private LeadEvent() { }

    private LeadEvent(Guid leadId, LeadEventKind kind)
    {
        LeadId = leadId;
        Kind = kind;
    }

    internal static LeadEvent Created(Guid leadId) => new(leadId, LeadEventKind.Created);

    internal static LeadEvent Resubmitted(Guid leadId) => new(leadId, LeadEventKind.Resubmitted);

    internal static LeadEvent StatusChanged(Guid leadId, LeadStatus from, LeadStatus to, string? reason) =>
        new(leadId, LeadEventKind.StatusChanged) { FromStatus = from, ToStatus = to, Text = reason };

    internal static LeadEvent Note(Guid leadId, string text) =>
        new(leadId, LeadEventKind.Note) { Text = text };
}

public enum LeadEventKind
{
    Created,
    Resubmitted,
    StatusChanged,
    Note
}
