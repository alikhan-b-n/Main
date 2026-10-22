using Lama.Application.BotAnalytics;
using Lama.Domain.BotAnalytics;

namespace Lama.Tests.BotAnalytics;

public class BotStatsCalculatorTests
{
    private static readonly DateTime Now = new(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

    private readonly List<SurveyEvent> _events = new();

    /// <summary>Writes one person's way through the bot, minute by minute from <paramref name="start"/>.</summary>
    private sealed class Script
    {
        private readonly List<SurveyEvent> _sink;
        private readonly string _session;
        private readonly long _telegramId;
        private DateTime _at;
        private string? _source;

        public Script(List<SurveyEvent> sink, string session, long telegramId, DateTime start, string? source)
        {
            _sink = sink;
            _session = session;
            _telegramId = telegramId;
            _at = start;
            _source = source;
        }

        public Script Wait(TimeSpan time)
        {
            _at += time;
            return this;
        }

        public Script Source(string source)
        {
            _source = source;
            return this;
        }

        public Script Add(SurveyEventKind kind, string? question = null, int? step = null, string? detail = null)
        {
            _sink.Add(SurveyEvent.Record(Guid.NewGuid(), _session, _telegramId, kind, _at,
                telegramUsername: "user" + _telegramId, source: _source,
                questionId: question, step: step, detail: detail));
            return this;
        }

        /// <summary>Question shown, then answered after <paramref name="seconds"/>.</summary>
        public Script Ask(string question, int step, int seconds) =>
            Add(SurveyEventKind.QuestionShown, question, step)
                .Wait(TimeSpan.FromSeconds(seconds))
                .Add(SurveyEventKind.Answered, question, step);
    }

    private Script Person(string session, long telegramId, DateTime start, string? source = "instagram") =>
        new(_events, session, telegramId, start, source);

    private BotStatsDto Stats(int days = 30, string? source = null, DateTime? since = null) =>
        BotStatsCalculator.Calculate(_events, Now, days, source, TimeZoneInfo.Utc,
            since ?? _events.Select(e => (DateTime?)e.OccurredAt).Min());

    [Fact]
    public void AFinishedSurveyCountsOnceAndItsDurationIsTheMedian()
    {
        Person("a", 1, Now.AddDays(-1))
            .Add(SurveyEventKind.BotStarted)
            .Add(SurveyEventKind.SurveyStarted)
            .Ask("applicant", 0, 10)
            .Ask("budget", 6, 50)
            .Add(SurveyEventKind.Completed, detail: "call");

        var stats = Stats();

        Assert.Equal(new BotStatsTotals(1, 1, 1, 0, 0, 60), stats.Totals);
        Assert.Equal("call", Assert.Single(stats.Outcomes).Key);
    }

    [Fact]
    public void SilenceForADayIsAbandonedAndLessIsStillInProgress()
    {
        Person("gone", 1, Now.AddDays(-3))
            .Add(SurveyEventKind.SurveyStarted).Ask("applicant", 0, 5).Add(SurveyEventKind.QuestionShown, "budget", 6);
        Person("thinking", 2, Now.AddHours(-2))
            .Add(SurveyEventKind.SurveyStarted).Add(SurveyEventKind.QuestionShown, "applicant", 0);

        var stats = Stats();

        Assert.Equal(1, stats.Totals.Abandoned);
        Assert.Equal(1, stats.Totals.InProgress);
        var abandoned = Assert.Single(stats.Abandoned);
        Assert.Equal("budget", abandoned.LastQuestionId);
        Assert.Equal(1, stats.Questions.Single(q => q.QuestionId == "budget").QuitHere);
    }

    [Fact]
    public void CancelIsAbandonedAtOnce()
    {
        Person("c", 1, Now.AddMinutes(-10))
            .Add(SurveyEventKind.SurveyStarted).Add(SurveyEventKind.QuestionShown, "applicant", 0)
            .Add(SurveyEventKind.Cancelled);

        Assert.True(Assert.Single(Stats().Abandoned).Cancelled);
    }

    [Fact]
    public void AReminderDoesNotKeepADeadSurveyAlive()
    {
        Person("r", 1, Now.AddDays(-2))
            .Add(SurveyEventKind.SurveyStarted).Add(SurveyEventKind.QuestionShown, "applicant", 0)
            .Wait(TimeSpan.FromHours(30)).Add(SurveyEventKind.ReminderSent);

        var stats = Stats();

        Assert.Equal(1, stats.Totals.Abandoned);
        Assert.True(Assert.Single(stats.Abandoned).Reminded);
        Assert.Equal(1, stats.Reminders.Sent);
    }

    [Fact]
    public void ACompletionAfterTheReminderIsCredited()
    {
        Person("r", 1, Now.AddDays(-2))
            .Add(SurveyEventKind.SurveyStarted).Add(SurveyEventKind.QuestionShown, "applicant", 0)
            .Wait(TimeSpan.FromHours(3)).Add(SurveyEventKind.ReminderSent)
            .Wait(TimeSpan.FromMinutes(5)).Add(SurveyEventKind.Resumed)
            .Ask("applicant", 0, 4).Add(SurveyEventKind.Completed, detail: "nurture");

        Assert.Equal(new ReminderStatDto(1, 1, 1), Stats().Reminders);
    }

    [Fact]
    public void FunnelFollowsTheBotsOrderAndCountsEachSessionOnce()
    {
        Person("a", 1, Now.AddDays(-1)).Add(SurveyEventKind.SurveyStarted)
            .Ask("applicant", 0, 5).Ask("budget", 6, 5)
            // Went back and saw the budget question again: still one session that reached it
            .Add(SurveyEventKind.Back, "budget", 6).Ask("budget", 6, 5);
        Person("b", 2, Now.AddDays(-1)).Add(SurveyEventKind.SurveyStarted).Ask("applicant", 0, 5);

        var questions = Stats().Questions;

        Assert.Equal(new[] { "applicant", "budget" }, questions.Select(q => q.QuestionId));
        Assert.Equal(2, questions[0].Reached);
        Assert.Equal(1, questions[1].Reached);
        Assert.Equal(1, questions[1].Backs);
    }

    [Fact]
    public void ThinkingTimeIsTheMedianAndLeavingForHoursDoesNotCount()
    {
        Person("a", 1, Now.AddDays(-1)).Add(SurveyEventKind.SurveyStarted).Ask("email", 9, 10);
        Person("b", 2, Now.AddDays(-1)).Add(SurveyEventKind.SurveyStarted).Ask("email", 9, 30);
        Person("c", 3, Now.AddDays(-1)).Add(SurveyEventKind.SurveyStarted).Ask("email", 9, 3 * 3600);

        var email = Stats().Questions.Single();

        Assert.Equal(20, email.MedianSeconds);
        Assert.Equal(3, email.Answered);
    }

    [Fact]
    public void BadAnswersAreCountedPerQuestion()
    {
        Person("a", 1, Now.AddDays(-1)).Add(SurveyEventKind.SurveyStarted)
            .Add(SurveyEventKind.QuestionShown, "email", 9)
            .Add(SurveyEventKind.InvalidInput, "email", 9, "invalid")
            .Add(SurveyEventKind.InvalidInput, "email", 9, "invalid")
            .Add(SurveyEventKind.Answered, "email", 9);

        Assert.Equal(2, Stats().Questions.Single().InvalidInputs);
    }

    [Fact]
    public void TheSourceFilterNarrowsTotalsButSourcesStayComparable()
    {
        Person("a", 1, Now.AddDays(-1), "instagram").Add(SurveyEventKind.SurveyStarted)
            .Add(SurveyEventKind.Completed, detail: "call");
        Person("b", 2, Now.AddDays(-1), "tiktok").Add(SurveyEventKind.SurveyStarted);

        var stats = Stats(source: "tiktok");

        Assert.Equal(1, stats.Totals.SurveysStarted);
        Assert.Equal(0, stats.Totals.Completed);
        Assert.Equal(2, stats.Sources.Count);
    }

    [Fact]
    public void TheSourceLearntAtTheEndAppliesToTheWholeSession()
    {
        Person("a", 1, Now.AddDays(-1), source: null).Add(SurveyEventKind.SurveyStarted)
            .Ask("applicant", 0, 5).Source("tiktok").Ask("source", 11, 5)
            .Add(SurveyEventKind.Completed, detail: "call");

        Assert.Equal("tiktok", Assert.Single(Stats().Sources).Source);
    }

    [Fact]
    public void SomeoneWhoFinishedLaterIsNotOnTheFollowUpList()
    {
        Person("first", 1, Now.AddDays(-5)).Add(SurveyEventKind.SurveyStarted)
            .Add(SurveyEventKind.QuestionShown, "applicant", 0);
        Person("second", 1, Now.AddDays(-2)).Add(SurveyEventKind.SurveyStarted)
            .Add(SurveyEventKind.Completed, detail: "call");

        var stats = Stats();

        Assert.Equal(1, stats.Totals.Abandoned);
        Assert.Empty(stats.Abandoned);
    }

    [Fact]
    public void SomeoneWhoGaveUpTwiceIsListedOnce()
    {
        Person("first", 1, Now.AddDays(-5)).Add(SurveyEventKind.SurveyStarted)
            .Add(SurveyEventKind.QuestionShown, "applicant", 0);
        Person("second", 1, Now.AddDays(-3)).Add(SurveyEventKind.SurveyStarted)
            .Add(SurveyEventKind.QuestionShown, "budget", 6);

        var listed = Assert.Single(Stats().Abandoned);
        Assert.Equal("second", listed.SessionId);
        Assert.Equal(1, Stats().AbandonedTotal);
    }

    [Fact]
    public void ThePreviousPeriodIsCountedForTheTrend()
    {
        Person("old", 1, Now.AddDays(-10)).Add(SurveyEventKind.BotStarted);
        Person("new", 2, Now.AddDays(-1)).Add(SurveyEventKind.BotStarted);

        var stats = Stats(days: 7);

        Assert.Equal(1, stats.Totals.BotStarts);
        Assert.Equal(1, stats.PreviousTotals.BotStarts);
    }

    [Fact]
    public void CollectedDaysAreCappedByThePeriodAndByTheFirstEvent()
    {
        Person("a", 1, Now.AddDays(-1)).Add(SurveyEventKind.BotStarted);

        Assert.Equal(2, Stats(days: 30, since: Now.AddDays(-1.5)).CollectedDays);
        Assert.Equal(7, Stats(days: 7, since: Now.AddDays(-40)).CollectedDays);
        Assert.Equal(0, BotStatsCalculator.Calculate(
            Array.Empty<SurveyEvent>(), Now, 30, null, TimeZoneInfo.Utc, null).CollectedDays);
    }

    [Fact]
    public void BookingStepsAreCountedPerSession()
    {
        Person("a", 1, Now.AddDays(-1)).Add(SurveyEventKind.Completed, detail: "call")
            .Add(SurveyEventKind.BookingOffered).Add(SurveyEventKind.BookingDayPicked)
            .Add(SurveyEventKind.BookingDayPicked).Add(SurveyEventKind.BookingBooked);
        Person("b", 2, Now.AddDays(-1)).Add(SurveyEventKind.Completed, detail: "call")
            .Add(SurveyEventKind.BookingOffered);

        Assert.Equal(new BookingFunnelDto(2, 1, 1, 0), Stats().Booking);
    }

    [Fact]
    public void StartsAreBucketedByTheViewersHour()
    {
        Person("a", 1, new DateTime(2026, 10, 21, 3, 30, 0, DateTimeKind.Utc)).Add(SurveyEventKind.SurveyStarted);

        var almaty = TimeZoneInfo.FindSystemTimeZoneById("Asia/Almaty");
        var stats = BotStatsCalculator.Calculate(_events, Now, 30, null, almaty, null);

        // 03:30 UTC is 08:30 in Almaty (UTC+5)
        Assert.Equal(1, stats.StartsByHour[8]);
    }
}
