using System.Text.Json.Serialization;
using Lama.Domain.LeadManagement.Entities;

namespace Lama.Api.Integrations.TelegramBot;

/// <summary>
/// The JSON the IconicU bot posts (Lead.to_crm_payload() in IconicUTelegramBot/app/crm.py).
/// Unknown fields such as "qualification_labels" are ignored.
/// </summary>
public record TelegramLeadPayload(
    [property: JsonPropertyName("lead_id")] string? LeadId,
    [property: JsonPropertyName("channel")] string? Channel,
    [property: JsonPropertyName("created_at")] DateTimeOffset? CreatedAt,
    [property: JsonPropertyName("lead_source")] string? LeadSource,
    [property: JsonPropertyName("contact")] TelegramLeadContact? Contact,
    [property: JsonPropertyName("qualification")] TelegramLeadQualification? Qualification,
    [property: JsonPropertyName("score")] int? Score,
    [property: JsonPropertyName("temperature")] string? Temperature,
    [property: JsonPropertyName("note")] string? Note
)
{
    public LeadSubmission ToSubmission(string rawPayload, DateTime receivedAtUtc)
    {
        var contact = Contact ?? new TelegramLeadContact(null, null, null, null, null);
        var q = Qualification ?? new TelegramLeadQualification();

        // Unknown or numeric values fall back to the score-based temperature in the domain.
        LeadTemperature? temperature =
            Enum.TryParse<LeadTemperature>(Temperature, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
                ? parsed
                : null;

        return new LeadSubmission(
            FullName: contact.FullName ?? string.Empty,
            Email: contact.Email ?? string.Empty,
            Phone: contact.Phone,
            TelegramUsername: contact.TelegramUsername,
            TelegramId: contact.TelegramId,
            Source: LeadSource,
            ExternalId: LeadId,
            Qualification: new LeadQualification(
                ApplicantType: q.ApplicantType,
                AgeRange: q.ApplicantAge,
                CurrentEducation: q.CurrentEducation,
                TargetDegree: q.TargetDegree,
                IntakeYear: q.IntakeYear,
                TargetCountries: q.TargetCountries,
                FundingNeed: q.FundingNeed,
                AnnualBudget: q.AnnualBudget,
                Gpa: q.Gpa,
                EnglishLevel: q.EnglishLevel,
                EnglishCertificate: q.EnglishCertificate,
                EnglishScore: q.EnglishScore,
                FieldsOfInterest: q.FieldsOfInterest,
                ServicesNeeded: q.ServicesNeeded),
            Score: Score ?? 0,
            Temperature: temperature,
            SurveySummary: Note,
            RawPayload: rawPayload,
            SubmittedAt: CreatedAt?.UtcDateTime ?? receivedAtUtc);
    }
}

public record TelegramLeadContact(
    [property: JsonPropertyName("full_name")] string? FullName,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("telegram_username")] string? TelegramUsername,
    [property: JsonPropertyName("telegram_id")] long? TelegramId
);

public record TelegramLeadQualification(
    [property: JsonPropertyName("applicant_type")] string? ApplicantType = null,
    [property: JsonPropertyName("applicant_age")] string? ApplicantAge = null,
    [property: JsonPropertyName("current_education")] string? CurrentEducation = null,
    [property: JsonPropertyName("target_degree")] string? TargetDegree = null,
    [property: JsonPropertyName("intake_year")] string? IntakeYear = null,
    [property: JsonPropertyName("target_countries")] List<string>? TargetCountries = null,
    [property: JsonPropertyName("funding_need")] string? FundingNeed = null,
    [property: JsonPropertyName("annual_budget")] string? AnnualBudget = null,
    [property: JsonPropertyName("gpa")] string? Gpa = null,
    [property: JsonPropertyName("english_level")] string? EnglishLevel = null,
    [property: JsonPropertyName("english_certificate")] string? EnglishCertificate = null,
    [property: JsonPropertyName("english_score")] string? EnglishScore = null,
    [property: JsonPropertyName("fields_of_interest")] string? FieldsOfInterest = null,
    [property: JsonPropertyName("services_needed")] List<string>? ServicesNeeded = null
);
