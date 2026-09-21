using Lama.Application.LeadManagement;
using Lama.Application.LeadManagement.Commands;
using Lama.Domain.LeadManagement.Entities;

namespace Lama.Tests.LeadManagement;

public class SubmitTelegramLeadCommandHandlerTests
{
    private readonly InMemoryLeadRepository _repository = new();
    private readonly SubmitTelegramLeadCommandHandler _handler;

    public SubmitTelegramLeadCommandHandlerTests()
    {
        _handler = new SubmitTelegramLeadCommandHandler(_repository);
    }

    private Task<SubmitLeadResult> Submit(LeadSubmission submission) =>
        _handler.Handle(new SubmitTelegramLeadCommand(submission), CancellationToken.None);

    [Fact]
    public async Task NewPerson_CreatesLead()
    {
        var result = await Submit(LeadSamples.Submission());

        Assert.Equal(SubmitLeadOutcome.Created, result.Outcome);
        Assert.Single(_repository.Leads);
    }

    [Fact]
    public async Task RetryWithSameLeadId_IsIgnored()
    {
        var first = await Submit(LeadSamples.Submission());

        var retry = await Submit(LeadSamples.Submission() with { Qualification = new LeadQualification(TargetDegree: "phd") });

        Assert.Equal(SubmitLeadOutcome.AlreadyProcessed, retry.Outcome);
        Assert.Equal(first.LeadId, retry.LeadId);
        Assert.Equal("master", _repository.Leads.Single().TargetDegree);
    }

    [Fact]
    public async Task SameTelegramUser_UpdatesOpenLead()
    {
        var first = await Submit(LeadSamples.Submission());

        var second = await Submit(LeadSamples.Submission() with
        {
            ExternalId = "bot-lead-2",
            Email = "other@mail.kz"
        });

        Assert.Equal(SubmitLeadOutcome.Updated, second.Outcome);
        Assert.Equal(first.LeadId, second.LeadId);
        Assert.Equal(2, _repository.Leads.Single().SubmissionCount);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task SameEmailFromAnotherAccount_UpdatesOpenLead()
    {
        await Submit(LeadSamples.Submission());

        var second = await Submit(LeadSamples.Submission() with
        {
            ExternalId = "bot-lead-2",
            TelegramId = 1,
            Email = "AIGERIM@gmail.com"
        });

        Assert.Equal(SubmitLeadOutcome.Updated, second.Outcome);
    }

    [Fact]
    public async Task ClosedLead_IsNotReused()
    {
        var first = await Submit(LeadSamples.Submission());
        _repository.Leads.Single().ChangeStatus(LeadStatus.Lost, "No budget");

        var second = await Submit(LeadSamples.Submission() with { ExternalId = "bot-lead-2" });

        Assert.Equal(SubmitLeadOutcome.Created, second.Outcome);
        Assert.NotEqual(first.LeadId, second.LeadId);
        Assert.Equal(2, _repository.Leads.Count);
    }

    private sealed class InMemoryLeadRepository : ILeadRepository
    {
        public List<Lead> Leads { get; } = new();
        public int SaveCount { get; private set; }

        public Task<Lead?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Leads.FirstOrDefault(l => l.Id == id));

        public Task<Lead?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Leads.FirstOrDefault(l => l.ExternalId == externalId));

        public Task<Lead?> FindOpenDuplicateAsync(long? telegramId, string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(Leads
                .Where(l => l.IsOpen)
                .FirstOrDefault(l => (telegramId != null && l.TelegramId == telegramId)
                                     || string.Equals(l.Email.Value, email.Trim(), StringComparison.OrdinalIgnoreCase)));

        public Task<PagedResult<Lead>> SearchAsync(LeadFilter filter, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<LeadStatsRow>> GetStatsRowsAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddAsync(Lead lead, CancellationToken cancellationToken = default)
        {
            Leads.Add(lead);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Lead lead, CancellationToken cancellationToken = default)
        {
            Leads.Remove(lead);
            return Task.CompletedTask;
        }
    }
}
