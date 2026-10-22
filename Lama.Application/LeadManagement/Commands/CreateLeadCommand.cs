using Lama.Application.Common;
using Lama.Domain.LeadManagement.Entities;

namespace Lama.Application.LeadManagement.Commands;

/// <summary>
/// A manager adds someone who never went through the bot — a phone call, a message, a
/// walk-in. There is no survey, so the manager sets the temperature instead of a score.
/// </summary>
public record CreateLeadCommand(
    string FullName,
    string Email,
    string? Phone = null,
    string? TelegramUsername = null,
    string? Source = null,
    LeadTemperature Temperature = LeadTemperature.Warm,
    string? Comment = null) : ICommand<Guid>;

public class CreateLeadCommandHandler : ICommandHandler<CreateLeadCommand, Guid>
{
    /// <summary>Source of a lead typed in by hand when the manager does not name one.</summary>
    public const string ManualSource = "manual";

    private readonly ILeadRepository _leads;
    private readonly TimeProvider _clock;

    public CreateLeadCommandHandler(ILeadRepository leads, TimeProvider clock)
    {
        _leads = leads;
        _clock = clock;
    }

    public async Task<Guid> Handle(CreateLeadCommand command, CancellationToken cancellationToken)
    {
        // The bot merges a second survey into the open lead; by hand that would silently
        // overwrite answers, so the manager is pointed to the existing lead instead
        var existing = await _leads.FindOpenDuplicateAsync(null, command.Email, cancellationToken);
        if (existing != null)
            throw new DuplicateLeadException(existing.Id);

        var lead = Lead.Create(new LeadSubmission(
            FullName: command.FullName,
            Email: command.Email,
            Phone: command.Phone,
            TelegramUsername: command.TelegramUsername,
            TelegramId: null,
            Source: string.IsNullOrWhiteSpace(command.Source) ? ManualSource : command.Source,
            ExternalId: null,
            Qualification: new LeadQualification(),
            Score: 0,
            Temperature: command.Temperature,
            SurveySummary: null,
            RawPayload: null,
            SubmittedAt: _clock.GetUtcNow().UtcDateTime));

        if (!string.IsNullOrWhiteSpace(command.Comment))
            lead.AddNote(command.Comment);

        await _leads.AddAsync(lead, cancellationToken);
        return lead.Id;
    }
}

public class DuplicateLeadException : Exception
{
    public Guid ExistingLeadId { get; }

    public DuplicateLeadException(Guid existingLeadId)
        : base("An open lead with this email already exists")
    {
        ExistingLeadId = existingLeadId;
    }
}
