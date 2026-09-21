using System.Text.Json;
using Lama.Api.Integrations.TelegramBot;
using Lama.Domain.LeadManagement.Entities;

namespace Lama.Tests.LeadManagement;

public class TelegramLeadPayloadTests
{
    private static readonly DateTime ReceivedAt = new(2026, 9, 22, 0, 0, 0, DateTimeKind.Utc);

    private static TelegramLeadPayload Parse(string json) =>
        JsonSerializer.Deserialize<TelegramLeadPayload>(json)!;

    [Fact]
    public void ToSubmission_MapsEveryBotField()
    {
        var submission = Parse(LeadSamples.BotPayloadJson).ToSubmission("{raw}", ReceivedAt);

        Assert.Equal("Айгерим Сапарова", submission.FullName);
        Assert.Equal("aigerim@gmail.com", submission.Email);
        Assert.Equal("+77011234567", submission.Phone);
        Assert.Equal("@aigerim", submission.TelegramUsername);
        Assert.Equal(424242, submission.TelegramId);
        Assert.Equal("instagram", submission.Source);
        Assert.Equal(11, submission.Score);
        Assert.Equal(LeadTemperature.Hot, submission.Temperature);
        Assert.Equal("Заявка из Telegram-бота", submission.SurveySummary);
        Assert.Equal("{raw}", submission.RawPayload);
        Assert.Equal(new DateTime(2026, 9, 21, 15, 56, 49, DateTimeKind.Utc), submission.SubmittedAt);

        var q = submission.Qualification;
        Assert.Equal("self", q.ApplicantType);
        Assert.Equal("18_20", q.AgeRange);
        Assert.Equal("bachelor_done", q.CurrentEducation);
        Assert.Equal("master", q.TargetDegree);
        Assert.Equal("2027", q.IntakeYear);
        Assert.Equal(new[] { "de", "pl" }, q.TargetCountries);
        Assert.Equal("partial", q.FundingNeed);
        Assert.Equal("3000_5000", q.AnnualBudget);
        Assert.Equal("3,2 из 4", q.Gpa);
        Assert.Equal("b2", q.EnglishLevel);
        Assert.Equal("ielts", q.EnglishCertificate);
        Assert.Equal("IELTS 7.0", q.EnglishScore);
        Assert.Equal("IT", q.FieldsOfInterest);
        Assert.Equal(new[] { "programs", "turnkey" }, q.ServicesNeeded);
    }

    [Fact]
    public void ToSubmission_BuildsAValidLead()
    {
        var submission = Parse(LeadSamples.BotPayloadJson).ToSubmission("{}", ReceivedAt);

        var lead = Lead.Create(submission);

        Assert.Equal("master", lead.TargetDegree);
        Assert.Equal(LeadTemperature.Hot, lead.Temperature);
    }

    [Theory]
    [InlineData("\"lukewarm\"")]
    [InlineData("\"5\"")]
    [InlineData("null")]
    public void ToSubmission_IgnoresUnknownTemperature(string temperature)
    {
        var json = $$"""{"contact":{"full_name":"A B","email":"a@b.kz"},"score":6,"temperature":{{temperature}}}""";

        var submission = Parse(json).ToSubmission("{}", ReceivedAt);

        Assert.Null(submission.Temperature);
        Assert.Equal(LeadTemperature.Warm, Lead.Create(submission).Temperature);
    }

    [Fact]
    public void ToSubmission_WithoutCreatedAt_UsesReceiveTime()
    {
        var submission = Parse("""{"contact":{"full_name":"A B","email":"a@b.kz"}}""").ToSubmission("{}", ReceivedAt);

        Assert.Equal(ReceivedAt, submission.SubmittedAt);
        Assert.Equal(0, submission.Score);
        Assert.Null(submission.Qualification.TargetCountries);
    }

    [Fact]
    public void ToSubmission_CarriesLeadIdForIdempotency()
    {
        var submission = Parse("""{"lead_id":"abc-1","contact":{"full_name":"A B","email":"a@b.kz"}}""")
            .ToSubmission("{}", ReceivedAt);

        Assert.Equal("abc-1", submission.ExternalId);
    }
}
