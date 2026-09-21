namespace Lama.Domain.LeadManagement.Entities;

/// <summary>One completed survey, as received from the Telegram bot.</summary>
public record LeadSubmission(
    string FullName,
    string Email,
    string? Phone,
    string? TelegramUsername,
    long? TelegramId,
    string? Source,
    string? ExternalId,
    LeadQualification Qualification,
    int Score,
    LeadTemperature? Temperature,
    string? SurveySummary,
    string? RawPayload,
    DateTime SubmittedAt
);

/// <summary>Survey answers as bot option codes.</summary>
public record LeadQualification(
    string? ApplicantType = null,
    string? AgeRange = null,
    string? CurrentEducation = null,
    string? TargetDegree = null,
    string? IntakeYear = null,
    IReadOnlyList<string>? TargetCountries = null,
    string? FundingNeed = null,
    string? AnnualBudget = null,
    string? Gpa = null,
    string? EnglishLevel = null,
    string? EnglishCertificate = null,
    string? EnglishScore = null,
    string? FieldsOfInterest = null,
    IReadOnlyList<string>? ServicesNeeded = null
);
