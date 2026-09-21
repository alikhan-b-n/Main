using Lama.Domain.LeadManagement.Entities;

namespace Lama.Application.LeadManagement;

public record LeadListItemDto(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    string? TelegramUsername,
    string Source,
    string? TargetDegree,
    IReadOnlyList<string> TargetCountries,
    string? AnnualBudget,
    string? FundingNeed,
    string? IntakeYear,
    int Score,
    string Temperature,
    string Status,
    string? NextStep,
    int SubmissionCount,
    DateTime CreatedAt,
    DateTime LastActivityAt
);

public record LeadDetailsDto(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    string? TelegramUsername,
    long? TelegramId,
    string Source,
    string? ApplicantType,
    string? AgeRange,
    string? CurrentEducation,
    string? TargetDegree,
    string? IntakeYear,
    IReadOnlyList<string> TargetCountries,
    string? FundingNeed,
    string? AnnualBudget,
    string? Gpa,
    string? EnglishLevel,
    string? EnglishCertificate,
    string? EnglishScore,
    string? FieldsOfInterest,
    IReadOnlyList<string> ServicesNeeded,
    string? UniversityPriority,
    int Score,
    string Temperature,
    string Status,
    string? NextStep,
    string? LostReason,
    string? SurveySummary,
    int SubmissionCount,
    DateTime SubmittedAt,
    DateTime CreatedAt,
    DateTime LastActivityAt,
    IReadOnlyList<LeadEventDto> Events
);

public record LeadEventDto(
    Guid Id,
    string Kind,
    string? FromStatus,
    string? ToStatus,
    string? Text,
    DateTime CreatedAt
);

internal static class LeadMapping
{
    // Timestamps are stored as UTC in `timestamp without time zone` and come back with
    // Kind=Unspecified, which serializes without a "Z" and makes browsers read them as local time.
    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    public static LeadListItemDto ToListItem(this Lead lead) => new(
        lead.Id,
        lead.FullName,
        lead.Email.Value,
        lead.Phone,
        lead.TelegramUsername,
        lead.Source,
        lead.TargetDegree,
        lead.TargetCountries,
        lead.AnnualBudget,
        lead.FundingNeed,
        lead.IntakeYear,
        lead.Score,
        lead.Temperature.ToString(),
        lead.Status.ToString(),
        lead.NextStep,
        lead.SubmissionCount,
        Utc(lead.CreatedAt),
        Utc(lead.LastActivityAt)
    );

    public static LeadDetailsDto ToDetails(this Lead lead) => new(
        lead.Id,
        lead.FullName,
        lead.Email.Value,
        lead.Phone,
        lead.TelegramUsername,
        lead.TelegramId,
        lead.Source,
        lead.ApplicantType,
        lead.AgeRange,
        lead.CurrentEducation,
        lead.TargetDegree,
        lead.IntakeYear,
        lead.TargetCountries,
        lead.FundingNeed,
        lead.AnnualBudget,
        lead.Gpa,
        lead.EnglishLevel,
        lead.EnglishCertificate,
        lead.EnglishScore,
        lead.FieldsOfInterest,
        lead.ServicesNeeded,
        lead.UniversityPriority,
        lead.Score,
        lead.Temperature.ToString(),
        lead.Status.ToString(),
        lead.NextStep,
        lead.LostReason,
        lead.SurveySummary,
        lead.SubmissionCount,
        Utc(lead.SubmittedAt),
        Utc(lead.CreatedAt),
        Utc(lead.LastActivityAt),
        lead.Events
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new LeadEventDto(
                e.Id,
                e.Kind.ToString(),
                e.FromStatus?.ToString(),
                e.ToStatus?.ToString(),
                e.Text,
                Utc(e.CreatedAt)))
            .ToList()
    );
}
