using Lama.Application.LeadManagement.Commands;
using Lama.Domain.LeadManagement.Entities;
using Lama.Tests.Scheduling;
using Microsoft.Extensions.Time.Testing;

namespace Lama.Tests.LeadManagement;

public class CreateLeadCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);

    private readonly FakeLeadRepository _leads = new();
    private readonly CreateLeadCommandHandler _handler;

    public CreateLeadCommandHandlerTests()
    {
        _handler = new CreateLeadCommandHandler(_leads, new FakeTimeProvider(Now));
    }

    private Task<Guid> Create(string email = "dana@example.kz", string? source = null, string? comment = null) =>
        _handler.Handle(new CreateLeadCommand(
            "Dana Serik", email, "+7 700 000 00 00", "@dana", source, LeadTemperature.Hot, comment),
            CancellationToken.None);

    [Fact]
    public async Task ALeadTypedInByHandStartsAsNewWithTheChosenTemperature()
    {
        var id = await Create();

        var lead = Assert.Single(_leads.Leads);
        Assert.Equal(id, lead.Id);
        Assert.Equal(LeadStatus.New, lead.Status);
        Assert.Equal(LeadTemperature.Hot, lead.Temperature);
        Assert.Equal("manual", lead.Source);
        Assert.Equal(Now.UtcDateTime, lead.SubmittedAt);
        Assert.Null(lead.TelegramId);
    }

    [Fact]
    public async Task TheManagersSourceIsKept()
    {
        await Create(source: "instagram");

        Assert.Equal("instagram", Assert.Single(_leads.Leads).Source);
    }

    [Fact]
    public async Task TheCommentBecomesANote()
    {
        await Create(comment: "Called about a master's in Germany");

        Assert.Contains(Assert.Single(_leads.Leads).Events,
            e => e.Text == "Called about a master's in Germany");
    }

    [Fact]
    public async Task AnOpenLeadWithTheSameEmailIsNotDuplicated()
    {
        var first = await Create();

        var duplicate = await Assert.ThrowsAsync<DuplicateLeadException>(() => Create("DANA@example.kz"));

        Assert.Equal(first, duplicate.ExistingLeadId);
        Assert.Single(_leads.Leads);
    }

    [Fact]
    public async Task AClosedLeadDoesNotBlockANewOne()
    {
        await Create();
        _leads.Leads.Single().ChangeStatus(LeadStatus.Lost, "went elsewhere");

        await Create();

        Assert.Equal(2, _leads.Leads.Count);
    }
}
