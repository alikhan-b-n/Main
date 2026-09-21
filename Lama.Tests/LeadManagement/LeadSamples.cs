using Lama.Domain.LeadManagement.Entities;

namespace Lama.Tests.LeadManagement;

internal static class LeadSamples
{
    /// <summary>A payload produced by the bot's Lead.to_crm_payload() (qualification_labels trimmed).</summary>
    public const string BotPayloadJson = """
        {
          "channel": "telegram_bot",
          "created_at": "2026-09-21T15:56:49+00:00",
          "lead_source": "instagram",
          "contact": {
            "full_name": "Айгерим Сапарова",
            "email": "aigerim@gmail.com",
            "phone": "+77011234567",
            "telegram_username": "@aigerim",
            "telegram_id": 424242
          },
          "qualification": {
            "applicant_type": "self",
            "applicant_age": "18_20",
            "current_education": "bachelor_done",
            "target_degree": "master",
            "intake_year": "2027",
            "target_countries": ["de", "pl"],
            "funding_need": "partial",
            "annual_budget": "3000_5000",
            "gpa": "3,2 из 4",
            "english_level": "b2",
            "english_certificate": "ielts",
            "english_score": "IELTS 7.0",
            "fields_of_interest": "IT",
            "services_needed": ["programs", "turnkey"],
            "full_name": "Айгерим Сапарова",
            "email": "aigerim@gmail.com",
            "phone": "+77011234567",
            "lead_source": "instagram"
          },
          "qualification_labels": { "applicant": "Я сам(а)" },
          "score": 11,
          "temperature": "hot",
          "note": "Заявка из Telegram-бота"
        }
        """;

    public static LeadSubmission Submission() => new(
        FullName: "Айгерим Сапарова",
        Email: "aigerim@gmail.com",
        Phone: "+77011234567",
        TelegramUsername: "@aigerim",
        TelegramId: 424242,
        Source: "instagram",
        ExternalId: "bot-lead-1",
        Qualification: new LeadQualification(
            TargetDegree: "master",
            TargetCountries: new[] { "de", "pl" },
            AnnualBudget: "3000_5000"),
        Score: 11,
        Temperature: LeadTemperature.Hot,
        SurveySummary: null,
        RawPayload: null,
        SubmittedAt: new DateTime(2026, 9, 21, 15, 56, 49, DateTimeKind.Utc));
}
