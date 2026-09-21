using Lama.Domain.LeadManagement.Entities;

namespace Lama.Tests.LeadManagement;

public class LeadTests
{
    [Fact]
    public void Create_StartsAsNewLeadWithCreatedEvent()
    {
        var lead = Lead.Create(LeadSamples.Submission());

        Assert.Equal(LeadStatus.New, lead.Status);
        Assert.Equal(1, lead.SubmissionCount);
        Assert.Equal("aigerim@gmail.com", lead.Email.Value);
        Assert.Equal(new[] { "de", "pl" }, lead.TargetCountries);
        var created = Assert.Single(lead.Events);
        Assert.Equal(LeadEventKind.Created, created.Kind);
        Assert.Equal(lead.Id, created.LeadId);
    }

    [Theory]
    [InlineData(11, LeadTemperature.Hot)]
    [InlineData(8, LeadTemperature.Hot)]
    [InlineData(5, LeadTemperature.Warm)]
    [InlineData(4, LeadTemperature.Cold)]
    public void Create_WithoutTemperature_DerivesItFromScore(int score, LeadTemperature expected)
    {
        var lead = Lead.Create(LeadSamples.Submission() with { Score = score, Temperature = null });

        Assert.Equal(expected, lead.Temperature);
    }

    [Fact]
    public void Create_NormalizesBlankValues()
    {
        var submission = LeadSamples.Submission() with
        {
            Phone = "  ",
            Source = "",
            Qualification = new LeadQualification(Gpa: "", TargetCountries: new[] { "de", " ", "de" })
        };

        var lead = Lead.Create(submission);

        Assert.Null(lead.Phone);
        Assert.Equal("telegram_bot", lead.Source);
        Assert.Null(lead.Gpa);
        Assert.Equal(new[] { "de" }, lead.TargetCountries);
    }

    [Fact]
    public void Create_RejectsInvalidEmail()
    {
        Assert.Throws<ArgumentException>(() => Lead.Create(LeadSamples.Submission() with { Email = "not-an-email" }));
    }

    [Fact]
    public void Resubmit_RefreshesAnswersAndKeepsKnownIds()
    {
        var lead = Lead.Create(LeadSamples.Submission());

        lead.Resubmit(LeadSamples.Submission() with
        {
            TelegramId = null,
            ExternalId = null,
            Qualification = new LeadQualification(TargetDegree: "phd")
        });

        Assert.Equal(2, lead.SubmissionCount);
        Assert.Equal("phd", lead.TargetDegree);
        Assert.Empty(lead.TargetCountries);
        Assert.Equal(424242, lead.TelegramId);
        Assert.Equal("bot-lead-1", lead.ExternalId);
        Assert.Equal(LeadEventKind.Resubmitted, lead.Events.Last().Kind);
    }

    [Fact]
    public void ChangeStatus_RecordsTransition()
    {
        var lead = Lead.Create(LeadSamples.Submission());

        lead.ChangeStatus(LeadStatus.Contacted);

        Assert.Equal(LeadStatus.Contacted, lead.Status);
        var change = lead.Events.Last();
        Assert.Equal(LeadEventKind.StatusChanged, change.Kind);
        Assert.Equal(LeadStatus.New, change.FromStatus);
        Assert.Equal(LeadStatus.Contacted, change.ToStatus);
    }

    [Fact]
    public void ChangeStatus_ToSameStatus_IsNoOp()
    {
        var lead = Lead.Create(LeadSamples.Submission());

        lead.ChangeStatus(LeadStatus.New);

        Assert.Single(lead.Events);
    }

    [Fact]
    public void ChangeStatus_ToLost_RequiresReason()
    {
        var lead = Lead.Create(LeadSamples.Submission());

        Assert.Throws<ArgumentException>(() => lead.ChangeStatus(LeadStatus.Lost, " "));

        lead.ChangeStatus(LeadStatus.Lost, " Budget too low ");
        Assert.Equal("Budget too low", lead.LostReason);
        Assert.False(lead.IsOpen);
    }

    [Fact]
    public void ChangeStatus_LeavingLost_ClearsReason()
    {
        var lead = Lead.Create(LeadSamples.Submission());
        lead.ChangeStatus(LeadStatus.Lost, "No budget");

        lead.ChangeStatus(LeadStatus.Contacted);

        Assert.Null(lead.LostReason);
        Assert.True(lead.IsOpen);
    }

    [Fact]
    public void AddNote_RejectsEmptyAndTooLongText()
    {
        var lead = Lead.Create(LeadSamples.Submission());

        Assert.Throws<ArgumentException>(() => lead.AddNote("   "));
        Assert.Throws<ArgumentException>(() => lead.AddNote(new string('x', LeadEvent.MaxTextLength + 1)));

        lead.AddNote("  Called back  ");
        Assert.Equal("Called back", lead.Events.Last().Text);
    }
}
