using System.Text.Json;
using Lama.Application.LeadManagement.Commands;
using Lama.Domain.LeadManagement.Entities;
using Lama.Tests.Scheduling;

namespace Lama.Tests.LeadManagement;

public class ImportLeadsCommandHandlerTests
{
    private static readonly DateTime Applied = new(2024, 1, 14, 19, 44, 13, DateTimeKind.Utc);

    private readonly FakeLeadRepository _leads = new();
    private readonly ImportLeadsCommandHandler _handler;

    public ImportLeadsCommandHandlerTests()
    {
        _handler = new ImportLeadsCommandHandler(_leads);
    }

    private static ImportLeadRow Row(string name = "Koskulakova Leila", string phone = "+77028639340",
        string email = "site-1@imported.invalid", IReadOnlyList<string>? notes = null) =>
        new(name, email, phone, null, "website_form", Applied, LeadStatus.Contacted, LeadTemperature.Cold,
            TargetDegree: "master", AnnualBudget: "lt_3000", SurveySummary: "Заявка с формы на сайте", Notes: notes);

    private Task<ImportLeadsResult> Import(bool dryRun, params ImportLeadRow[] rows) =>
        _handler.Handle(new ImportLeadsCommand(rows, dryRun), CancellationToken.None);

    [Fact]
    public async Task AnImportedLeadIsDatedWhenThePersonApplied()
    {
        await Import(false, Row(notes: new[] { "хотела бесплатно" }));

        var lead = Assert.Single(_leads.Leads);
        Assert.Equal(Applied, lead.CreatedAt);
        Assert.Equal(Applied, lead.SubmittedAt);
        Assert.Equal(Applied, lead.LastActivityAt);
        Assert.Equal(LeadStatus.Contacted, lead.Status);
        Assert.Equal(LeadTemperature.Cold, lead.Temperature);
        Assert.All(lead.Events, e => Assert.Equal(Applied, e.CreatedAt));
        Assert.Contains(lead.Events, e => e.Kind == LeadEventKind.Note && e.Text == "хотела бесплатно");
        Assert.Contains(lead.Events, e => e.Kind == LeadEventKind.StatusChanged && e.ToStatus == LeadStatus.Contacted);
    }

    [Fact]
    public async Task APhoneAlreadyInTheCrmIsSkippedWhateverItsFormat()
    {
        var existing = Lead.Create(LeadSamples.Submission() with { Phone = "8 702 863 93 40" });
        _leads.Leads.Add(existing);

        var result = await Import(false, Row(phone: "+7 (702) 863-93-40"));

        Assert.Equal(0, result.Created);
        var skip = Assert.Single(result.Skipped);
        Assert.Equal("phone_in_crm", skip.Reason);
        Assert.Equal(existing.Id, skip.ExistingLeadId);
        Assert.Single(_leads.Leads);
    }

    [Fact]
    public async Task AnEmailAlreadyInTheCrmIsSkipped()
    {
        _leads.Leads.Add(Lead.Create(LeadSamples.Submission()));

        var result = await Import(false, Row(phone: "+79990001122", email: "AIGERIM@gmail.com"));

        Assert.Equal("email_in_crm", Assert.Single(result.Skipped).Reason);
    }

    [Fact]
    public async Task ARepeatInsideTheBatchIsCreatedOnce()
    {
        var result = await Import(false, Row(), Row(name: "Leila K.", email: "site-2@imported.invalid"));

        Assert.Equal(1, result.Created);
        Assert.Equal("repeated_in_batch", Assert.Single(result.Skipped).Reason);
    }

    [Fact]
    public async Task ADryRunWritesNothingButReportsTheSame()
    {
        var result = await Import(true, Row());

        Assert.True(result.DryRun);
        Assert.Equal(1, result.Created);
        Assert.Empty(_leads.Leads);
    }

    [Fact]
    public async Task ARowWithoutAPhoneIsStillImported()
    {
        var result = await Import(false, Row(phone: "не пользуюсь приложением."));

        Assert.Equal(1, result.Created);
    }

    [Fact]
    public void ALeadCannotBeImportedAsLost()
    {
        var submission = LeadSamples.Submission() with { SubmittedAt = Applied };
        Assert.Throws<ArgumentException>(() => Lead.Import(submission, LeadStatus.Lost, Array.Empty<string>()));
    }

    [Theory]
    [InlineData("87011234567", "77011234567")]
    [InlineData("+7 701 123 45 67", "77011234567")]
    [InlineData("7011234567", "77011234567")]
    [InlineData("+375 44 575 24 32", "375445752432")]
    [InlineData("+375", null)]
    [InlineData(null, null)]
    public void PhonesAreComparedByDigits(string? phone, string? key) =>
        Assert.Equal(key, ImportLeadsCommandHandler.PhoneKey(phone));

    [Fact]
    public void StatusAndTemperatureAreReadAsNames()
    {
        var row = JsonSerializer.Deserialize<ImportLeadRow>(
            """{"fullName":"A B","email":"a@b.kz","source":"website_form","submittedAt":"2024-01-14T19:44:13Z","status":"Contacted","temperature":"Cold"}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        Assert.Equal(LeadStatus.Contacted, row.Status);
        Assert.Equal(LeadTemperature.Cold, row.Temperature);
    }
}
