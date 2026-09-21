using Lama.Application.Common;
using Lama.Domain.LeadManagement.Entities;

namespace Lama.Application.LeadManagement.Commands;

/// <summary>A completed survey from the Telegram bot.</summary>
public record SubmitTelegramLeadCommand(LeadSubmission Submission) : ICommand<SubmitLeadResult>;

public record SubmitLeadResult(Guid LeadId, SubmitLeadOutcome Outcome);

public enum SubmitLeadOutcome
{
    /// <summary>A new lead was created.</summary>
    Created,
    /// <summary>An open lead of the same person got the fresh answers.</summary>
    Updated,
    /// <summary>The bot retried a submission that was already stored; nothing changed.</summary>
    AlreadyProcessed
}

public class SubmitTelegramLeadCommandHandler : ICommandHandler<SubmitTelegramLeadCommand, SubmitLeadResult>
{
    private readonly ILeadRepository _leads;

    public SubmitTelegramLeadCommandHandler(ILeadRepository leads)
    {
        _leads = leads;
    }

    public async Task<SubmitLeadResult> Handle(SubmitTelegramLeadCommand command, CancellationToken cancellationToken)
    {
        var submission = command.Submission;

        if (!string.IsNullOrWhiteSpace(submission.ExternalId))
        {
            var processed = await _leads.FindByExternalIdAsync(submission.ExternalId.Trim(), cancellationToken);
            if (processed != null)
                return new SubmitLeadResult(processed.Id, SubmitLeadOutcome.AlreadyProcessed);
        }

        var existing = await _leads.FindOpenDuplicateAsync(submission.TelegramId, submission.Email, cancellationToken);
        if (existing != null)
        {
            existing.Resubmit(submission);
            await _leads.SaveChangesAsync(cancellationToken);
            return new SubmitLeadResult(existing.Id, SubmitLeadOutcome.Updated);
        }

        var lead = Lead.Create(submission);
        await _leads.AddAsync(lead, cancellationToken);
        return new SubmitLeadResult(lead.Id, SubmitLeadOutcome.Created);
    }
}
