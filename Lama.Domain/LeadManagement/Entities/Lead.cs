using Lama.Domain.Common;
using Lama.Domain.CustomerManagement.ValueObjects;

namespace Lama.Domain.LeadManagement.Entities;

/// <summary>
/// A prospective student who came in through the IconicU Telegram bot.
/// Qualification fields hold the bot's option codes (see IconicUTelegramBot/app/survey.py);
/// the UI translates them, so the codes must stay stable.
/// </summary>
public class Lead : AggregateRoot
{
    // Contact
    public string FullName { get; private set; }
    public Email Email { get; private set; }
    public string? Phone { get; private set; }
    public string? TelegramUsername { get; private set; }
    public long? TelegramId { get; private set; }

    // Origin
    public string Source { get; private set; }
    public string? ExternalId { get; private set; }

    // Qualification (bot option codes)
    public string? ApplicantType { get; private set; }
    public string? AgeRange { get; private set; }
    public string? CurrentEducation { get; private set; }
    public string? TargetDegree { get; private set; }
    public string? IntakeYear { get; private set; }
    public List<string> TargetCountries { get; private set; } = new();
    public string? FundingNeed { get; private set; }
    public string? AnnualBudget { get; private set; }
    public string? Gpa { get; private set; }
    public string? EnglishLevel { get; private set; }
    public string? EnglishCertificate { get; private set; }
    public string? EnglishScore { get; private set; }
    public string? FieldsOfInterest { get; private set; }
    public List<string> ServicesNeeded { get; private set; } = new();

    public int Score { get; private set; }
    public LeadTemperature Temperature { get; private set; }

    // Funnel
    public LeadStatus Status { get; private set; }
    public string? LostReason { get; private set; }

    // Audit
    public string? SurveySummary { get; private set; }
    public string? RawPayload { get; private set; }
    public DateTime SubmittedAt { get; private set; }
    public int SubmissionCount { get; private set; }
    public DateTime LastActivityAt { get; private set; }

    private readonly List<LeadEvent> _events = new();
    public IReadOnlyCollection<LeadEvent> Events => _events.AsReadOnly();

    public bool IsOpen => Status is not (LeadStatus.ContractSigned or LeadStatus.Lost);

    // Required members are populated by EF when loading, or by ApplySubmission in Create.
    private Lead()
    {
        FullName = null!;
        Email = null!;
        Source = null!;
    }

    public static Lead Create(LeadSubmission submission)
    {
        var lead = new Lead { Status = LeadStatus.New };
        lead.ApplySubmission(submission);
        lead._events.Add(LeadEvent.Created(lead.Id));
        return lead;
    }

    /// <summary>The same person filled in the survey again: refresh their answers.</summary>
    public void Resubmit(LeadSubmission submission)
    {
        ApplySubmission(submission);
        _events.Add(LeadEvent.Resubmitted(Id));
    }

    public void ChangeStatus(LeadStatus status, string? lostReason = null)
    {
        if (status == LeadStatus.Lost && string.IsNullOrWhiteSpace(lostReason))
            throw new ArgumentException("A reason is required when a lead is lost", nameof(lostReason));
        if (status == Status)
            return;

        var previous = Status;
        Status = status;
        LostReason = status == LeadStatus.Lost ? lostReason!.Trim() : null;
        Touch();
        _events.Add(LeadEvent.StatusChanged(Id, previous, status, LostReason));
    }

    public void AddNote(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Note cannot be empty", nameof(text));
        if (text.Trim().Length > LeadEvent.MaxTextLength)
            throw new ArgumentException($"Note must not exceed {LeadEvent.MaxTextLength} characters", nameof(text));

        Touch();
        _events.Add(LeadEvent.Note(Id, text.Trim()));
    }

    private void ApplySubmission(LeadSubmission submission)
    {
        if (string.IsNullOrWhiteSpace(submission.FullName))
            throw new ArgumentException("Full name cannot be empty", nameof(submission));
        if (submission.Score < 0)
            throw new ArgumentException("Score cannot be negative", nameof(submission));

        FullName = submission.FullName.Trim();
        Email = Email.Create(submission.Email.Trim());
        Phone = NullIfBlank(submission.Phone);
        TelegramUsername = NullIfBlank(submission.TelegramUsername);
        TelegramId = submission.TelegramId ?? TelegramId;

        Source = NullIfBlank(submission.Source) ?? "telegram_bot";
        ExternalId = NullIfBlank(submission.ExternalId) ?? ExternalId;

        var q = submission.Qualification;
        ApplicantType = NullIfBlank(q.ApplicantType);
        AgeRange = NullIfBlank(q.AgeRange);
        CurrentEducation = NullIfBlank(q.CurrentEducation);
        TargetDegree = NullIfBlank(q.TargetDegree);
        IntakeYear = NullIfBlank(q.IntakeYear);
        TargetCountries = Clean(q.TargetCountries);
        FundingNeed = NullIfBlank(q.FundingNeed);
        AnnualBudget = NullIfBlank(q.AnnualBudget);
        Gpa = NullIfBlank(q.Gpa);
        EnglishLevel = NullIfBlank(q.EnglishLevel);
        EnglishCertificate = NullIfBlank(q.EnglishCertificate);
        EnglishScore = NullIfBlank(q.EnglishScore);
        FieldsOfInterest = NullIfBlank(q.FieldsOfInterest);
        ServicesNeeded = Clean(q.ServicesNeeded);

        Score = submission.Score;
        Temperature = submission.Temperature ?? LeadTemperatureRules.FromScore(submission.Score);

        SurveySummary = NullIfBlank(submission.SurveySummary);
        RawPayload = submission.RawPayload;
        SubmittedAt = submission.SubmittedAt;
        SubmissionCount++;
        Touch();
    }

    private void Touch()
    {
        LastActivityAt = DateTime.UtcNow;
        UpdatedAt = LastActivityAt;
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static List<string> Clean(IEnumerable<string>? values) =>
        values?.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).Distinct().ToList() ?? new();
}

/// <summary>Funnel stages for IconicU consultations.</summary>
public enum LeadStatus
{
    New,
    Contacted,
    ConsultationScheduled,
    ConsultationDone,
    ContractSigned,
    Lost
}

public enum LeadTemperature
{
    Cold,
    Warm,
    Hot
}

public static class LeadTemperatureRules
{
    // Same thresholds as the bot's temperature_for(): hot >= 8, warm >= 5.
    public static LeadTemperature FromScore(int score) => score switch
    {
        >= 8 => LeadTemperature.Hot,
        >= 5 => LeadTemperature.Warm,
        _ => LeadTemperature.Cold
    };
}
