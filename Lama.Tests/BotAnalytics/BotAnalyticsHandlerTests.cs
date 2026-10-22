using Lama.Application.BotAnalytics;
using Lama.Domain.BotAnalytics;

namespace Lama.Tests.BotAnalytics;

public class BotAnalyticsHandlerTests
{
    private sealed class InMemoryEvents : ISurveyEventRepository
    {
        public List<SurveyEvent> Stored { get; } = new();

        public Task<int> AddNewAsync(IReadOnlyCollection<SurveyEvent> events, CancellationToken cancellationToken = default)
        {
            var fresh = events.Where(e => Stored.All(s => s.Id != e.Id)).DistinctBy(e => e.Id).ToList();
            Stored.AddRange(fresh);
            return Task.FromResult(fresh.Count);
        }

        public Task<IReadOnlyList<SurveyEvent>> GetBetweenAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SurveyEvent>>(Stored);

        public Task<DateTime?> GetFirstEventAtAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored.Select(e => (DateTime?)e.OccurredAt).Min());
    }

    private readonly InMemoryEvents _events = new();

    private static SurveyEventInput Input(Guid id, string kind) =>
        new(id, "session-1", 42, kind, new DateTime(2026, 10, 22, 9, 0, 0, DateTimeKind.Utc));

    [Theory]
    [InlineData("question_shown", SurveyEventKind.QuestionShown)]
    [InlineData("QuestionShown", SurveyEventKind.QuestionShown)]
    [InlineData("booking_day_picked", SurveyEventKind.BookingDayPicked)]
    public void TheBotsSnakeCaseKindsAreUnderstood(string value, SurveyEventKind expected)
    {
        Assert.True(RecordSurveyEventsCommandHandler.TryParseKind(value, out var kind));
        Assert.Equal(expected, kind);
    }

    [Fact]
    public async Task AResentBatchIsStoredOnce()
    {
        var handler = new RecordSurveyEventsCommandHandler(_events);
        var batch = new RecordSurveyEventsCommand(new[] { Input(Guid.NewGuid(), "bot_started"), Input(Guid.NewGuid(), "survey_started") });

        var first = await handler.Handle(batch, CancellationToken.None);
        var again = await handler.Handle(batch, CancellationToken.None);

        Assert.Equal(2, first.Stored);
        Assert.Equal(0, again.Stored);
        Assert.Equal(2, _events.Stored.Count);
    }

    [Fact]
    public async Task UnknownKindsAreDroppedWithoutLosingTheRest()
    {
        var handler = new RecordSurveyEventsCommandHandler(_events);

        var result = await handler.Handle(new RecordSurveyEventsCommand(new[]
        {
            Input(Guid.NewGuid(), "something_new"),
            Input(Guid.Empty, "bot_started"),
            Input(Guid.NewGuid(), "bot_started")
        }), CancellationToken.None);

        Assert.Equal(new RecordSurveyEventsResult(1, 2), result);
    }

    [Fact]
    public void OverlongCodesAreClippedNotRejected()
    {
        var e = SurveyEvent.Record(Guid.NewGuid(), "s", 1, SurveyEventKind.Answered, DateTime.UtcNow,
            questionId: new string('q', 200));

        Assert.Equal(SurveyEvent.MaxCodeLength, e.QuestionId!.Length);
    }

    [Fact]
    public void ReminderSettingsRejectAnImpossibleDelayOrZone()
    {
        Assert.Throws<ArgumentException>(() =>
            BotReminderSettings.Create(true, 0, new TimeOnly(22, 0), new TimeOnly(9, 0), "Asia/Almaty"));
        Assert.Throws<ArgumentException>(() =>
            BotReminderSettings.Create(true, 3, new TimeOnly(22, 0), new TimeOnly(9, 0), "Mars/Olympus"));
    }

    [Fact]
    public void RemindersStartSwitchedOnWithThreeHoursAndQuietNights()
    {
        var dto = GetBotReminderSettingsQueryHandler.ToDto(BotReminderSettings.Default());

        Assert.Equal(new BotReminderSettingsDto(true, 3, "22:00", "09:00", "Asia/Almaty"), dto);
    }
}
