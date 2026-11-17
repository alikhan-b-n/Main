using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FluentValidation;
using Lama.Application.Common;
using Lama.Domain.LeadManagement.Entities;

namespace Lama.Application.LeadManagement.Commands;

/// <summary>
/// Old applications carried over in bulk, such as the website form's answer sheet.
/// Each row already holds the CRM's codes; this command decides what is new.
/// A row whose phone or email belongs to a lead already in the CRM — from any source —
/// is skipped and reported, never merged: the manager decides what to do with it.
/// </summary>
/// <param name="DryRun">Report what would happen and write nothing.</param>
public record ImportLeadsCommand(IReadOnlyList<ImportLeadRow> Rows, bool DryRun) : ICommand<ImportLeadsResult>;

public record ImportLeadRow(
    string FullName,
    string Email,
    string? Phone,
    string? TelegramUsername,
    string Source,
    DateTime SubmittedAt,
    // Names ("Contacted", "Cold") rather than numbers: the rows are written by people and scripts
    [property: JsonConverter(typeof(JsonStringEnumConverter))] LeadStatus Status,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] LeadTemperature Temperature,
    string? CurrentEducation = null,
    string? TargetDegree = null,
    string? IntakeYear = null,
    IReadOnlyList<string>? TargetCountries = null,
    string? AnnualBudget = null,
    string? Gpa = null,
    string? EnglishLevel = null,
    string? FieldsOfInterest = null,
    IReadOnlyList<string>? ServicesNeeded = null,
    string? SurveySummary = null,
    IReadOnlyList<string>? Notes = null);

public record ImportLeadsResult(
    bool DryRun,
    int Created,
    IReadOnlyList<string> CreatedNames,
    IReadOnlyList<ImportSkip> Skipped);

/// <param name="ExistingLeadId">The lead it collides with; null when the clash is inside the batch.</param>
public record ImportSkip(string FullName, string? Phone, string Reason, Guid? ExistingLeadId, string? ExistingName);

public class ImportLeadsCommandHandler : ICommandHandler<ImportLeadsCommand, ImportLeadsResult>
{
    private readonly ILeadRepository _leads;

    public ImportLeadsCommandHandler(ILeadRepository leads)
    {
        _leads = leads;
    }

    public async Task<ImportLeadsResult> Handle(ImportLeadsCommand command, CancellationToken cancellationToken)
    {
        var contacts = await _leads.GetContactsAsync(cancellationToken);
        var byPhone = new Dictionary<string, LeadContact>();
        var byEmail = new Dictionary<string, LeadContact>(StringComparer.OrdinalIgnoreCase);
        foreach (var contact in contacts)
        {
            if (PhoneKey(contact.Phone) is { } phone)
                byPhone.TryAdd(phone, contact);
            byEmail.TryAdd(contact.Email.Trim(), contact);
        }

        var seenPhones = new HashSet<string>();
        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var created = new List<string>();
        var skipped = new List<ImportSkip>();

        foreach (var row in command.Rows)
        {
            var phone = PhoneKey(row.Phone);
            var email = row.Email.Trim();

            if (phone != null && byPhone.TryGetValue(phone, out var samePhone))
            {
                skipped.Add(new ImportSkip(row.FullName, row.Phone, "phone_in_crm", samePhone.Id, samePhone.FullName));
                continue;
            }
            if (byEmail.TryGetValue(email, out var sameEmail))
            {
                skipped.Add(new ImportSkip(row.FullName, row.Phone, "email_in_crm", sameEmail.Id, sameEmail.FullName));
                continue;
            }
            if ((phone != null && !seenPhones.Add(phone)) || !seenEmails.Add(email))
            {
                skipped.Add(new ImportSkip(row.FullName, row.Phone, "repeated_in_batch", null, null));
                continue;
            }

            var lead = Lead.Import(ToSubmission(row), row.Status, row.Notes ?? Array.Empty<string>());
            if (!command.DryRun)
                await _leads.AddAsync(lead, cancellationToken);
            created.Add(row.FullName);
        }

        return new ImportLeadsResult(command.DryRun, created.Count, created, skipped);
    }

    private static LeadSubmission ToSubmission(ImportLeadRow row) => new(
        FullName: row.FullName,
        Email: row.Email,
        Phone: row.Phone,
        TelegramUsername: row.TelegramUsername,
        TelegramId: null,
        Source: row.Source,
        ExternalId: null,
        Qualification: new LeadQualification(
            CurrentEducation: row.CurrentEducation,
            TargetDegree: row.TargetDegree,
            IntakeYear: row.IntakeYear,
            TargetCountries: row.TargetCountries,
            AnnualBudget: row.AnnualBudget,
            Gpa: row.Gpa,
            EnglishLevel: row.EnglishLevel,
            FieldsOfInterest: row.FieldsOfInterest,
            ServicesNeeded: row.ServicesNeeded),
        Score: 0,
        Temperature: row.Temperature,
        SurveySummary: row.SurveySummary,
        RawPayload: null,
        SubmittedAt: row.SubmittedAt);

    /// <summary>
    /// Digits only, with a Kazakh 8 turned into 7 — "+7 701 123", "87011…" and "7011…"
    /// are the same person. Too short to be a phone number means no key at all.
    /// </summary>
    public static string? PhoneKey(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;
        var digits = Regex.Replace(phone, @"\D", string.Empty);
        if (digits.Length == 11 && digits[0] == '8')
            digits = "7" + digits[1..];
        if (digits.Length == 10 && digits[0] == '7')
            digits = "7" + digits;
        return digits.Length >= 9 ? digits : null;
    }
}

public class ImportLeadsCommandValidator : AbstractValidator<ImportLeadsCommand>
{
    public const int MaxRows = 500;
    private const int CodeLength = 64;
    private const int FreeTextLength = 300;

    public ImportLeadsCommandValidator()
    {
        RuleFor(x => x.Rows).NotNull()
            .Must(r => r.Count <= MaxRows).WithMessage($"No more than {MaxRows} rows per import");

        RuleForEach(x => x.Rows).ChildRules(row =>
        {
            row.RuleFor(r => r.FullName).NotEmpty().MaximumLength(100);
            row.RuleFor(r => r.Email).NotEmpty().EmailAddress().MaximumLength(255);
            row.RuleFor(r => r.Phone).MaximumLength(20);
            row.RuleFor(r => r.TelegramUsername).MaximumLength(CodeLength);
            row.RuleFor(r => r.Source).NotEmpty().MaximumLength(CodeLength);
            row.RuleFor(r => r.Status).IsInEnum().NotEqual(LeadStatus.Lost);
            row.RuleFor(r => r.Temperature).IsInEnum();
            row.RuleFor(r => r.SubmittedAt).NotEmpty();
            row.RuleFor(r => r.CurrentEducation).MaximumLength(CodeLength);
            row.RuleFor(r => r.TargetDegree).MaximumLength(CodeLength);
            row.RuleFor(r => r.IntakeYear).MaximumLength(CodeLength);
            row.RuleFor(r => r.AnnualBudget).MaximumLength(CodeLength);
            row.RuleFor(r => r.EnglishLevel).MaximumLength(CodeLength);
            row.RuleFor(r => r.Gpa).MaximumLength(FreeTextLength);
            row.RuleFor(r => r.FieldsOfInterest).MaximumLength(FreeTextLength);
            row.RuleFor(r => r.SurveySummary).MaximumLength(4000);
            row.RuleForEach(r => r.TargetCountries).MaximumLength(CodeLength);
            row.RuleForEach(r => r.ServicesNeeded).MaximumLength(CodeLength);
        });
    }
}
