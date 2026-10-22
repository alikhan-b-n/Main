using Lama.Domain.BotAnalytics;

namespace Lama.Application.BotAnalytics;

/// <summary>
/// Turns the bot's raw events into the numbers of the statistics page.
/// Pure on purpose — events and a clock in, figures out — so the counting rules
/// (what is abandoned, what time counts as thinking) are pinned by tests.
/// </summary>
public static class BotStatsCalculator
{
    /// <summary>No answer for this long and the survey counts as dropped, not as still going.</summary>
    public static readonly TimeSpan AbandonAfter = TimeSpan.FromHours(24);

    /// <summary>
    /// Longer than this between a question and its answer is someone who left and came
    /// back, not someone thinking; it would only blur the median.
    /// </summary>
    public static readonly TimeSpan MaxThinkingTime = TimeSpan.FromMinutes(30);

    public const string UnknownKey = "unknown";
    public const int AbandonedListSize = 50;

    public static BotStatsDto Calculate(
        IEnumerable<SurveyEvent> events,
        DateTime nowUtc,
        int days,
        string? source,
        TimeZoneInfo viewerZone,
        DateTime? collectingSinceUtc)
    {
        var to = nowUtc;
        var from = to.AddDays(-days);
        var previousFrom = from.AddDays(-days);

        var sessions = events
            .GroupBy(e => e.SessionId)
            .Select(g => Session.From(g.OrderBy(e => e.OccurredAt).ToList(), nowUtc))
            .ToList();

        var current = sessions.Where(s => s.StartedAt >= from && s.StartedAt < to).ToList();
        var previous = sessions.Where(s => s.StartedAt >= previousFrom && s.StartedAt < from).ToList();

        // Sources are compared side by side, so that block ignores the source filter
        var sources = current
            .Where(s => s.SurveyStartedAt != null)
            .GroupBy(s => s.SourceKey)
            .Select(g => new SourceStatDto(g.Key, g.Count(), g.Count(s => s.IsCompleted)))
            .OrderByDescending(s => s.Started)
            .ToList();

        if (!string.IsNullOrWhiteSpace(source))
        {
            current = current.Where(s => s.SourceKey == source).ToList();
            previous = previous.Where(s => s.SourceKey == source).ToList();
        }

        // The list is for following people up: whoever finished in a later session is
        // already a lead, and someone who gave up twice is still one person to write to
        var finishedLater = sessions
            .Where(s => s.IsCompleted)
            .GroupBy(s => s.TelegramId)
            .ToDictionary(g => g.Key, g => g.Max(s => s.CompletedAt!.Value));
        var abandoned = current
            .Where(s => s.State == SessionState.Abandoned)
            .Where(s => !finishedLater.TryGetValue(s.TelegramId, out var done) || done < s.StartedAt)
            .GroupBy(s => s.TelegramId)
            .Select(g => g.MaxBy(s => s.LastActivityAt)!)
            .OrderByDescending(s => s.LastActivityAt)
            .ToList();

        return new BotStatsDto(
            days,
            from,
            to,
            collectingSinceUtc,
            CollectedDays(collectingSinceUtc, from, to, days),
            Totals(current),
            Totals(previous),
            Questions(current),
            current
                .Where(s => s.IsCompleted)
                .GroupBy(s => s.Outcome ?? UnknownKey)
                .Select(g => new CountDto(g.Key, g.Count()))
                .OrderByDescending(c => c.Count)
                .ToList(),
            sources,
            new BookingFunnelDto(
                current.Count(s => s.Has(SurveyEventKind.BookingOffered)),
                current.Count(s => s.Has(SurveyEventKind.BookingDayPicked)),
                current.Count(s => s.Has(SurveyEventKind.BookingBooked)),
                current.Count(s => s.Has(SurveyEventKind.BookingCancelled))),
            new ReminderStatDto(
                current.Count(s => s.RemindedAt != null),
                current.Count(s => s.Has(SurveyEventKind.Resumed)),
                current.Count(s => s.RemindedAt != null && s.CompletedAt > s.RemindedAt)),
            StartsByHour(current, viewerZone),
            abandoned.Take(AbandonedListSize).Select(s => s.ToAbandoned()).ToList(),
            abandoned.Count);
    }

    private static int CollectedDays(DateTime? collectingSince, DateTime from, DateTime to, int days)
    {
        if (collectingSince is not { } since || since >= to)
            return 0;

        var start = since > from ? since : from;
        return Math.Min(days, (int)Math.Ceiling((to - start).TotalDays));
    }

    private static BotStatsTotals Totals(IReadOnlyCollection<Session> sessions) => new(
        sessions.Count(s => s.Has(SurveyEventKind.BotStarted)),
        sessions.Count(s => s.SurveyStartedAt != null),
        sessions.Count(s => s.IsCompleted),
        sessions.Count(s => s.State == SessionState.Abandoned),
        sessions.Count(s => s.State == SessionState.InProgress),
        Median(sessions
            .Where(s => s.IsCompleted && s.SurveyStartedAt != null)
            .Select(s => (s.CompletedAt!.Value - s.SurveyStartedAt!.Value).TotalSeconds)));

    private static List<QuestionStatDto> Questions(IReadOnlyCollection<Session> sessions)
    {
        var stats = new Dictionary<string, QuestionTally>();
        QuestionTally Tally(string id) =>
            stats.TryGetValue(id, out var tally) ? tally : stats[id] = new QuestionTally();

        foreach (var session in sessions)
        {
            var shownAt = new Dictionary<string, DateTime>();
            foreach (var e in session.Events.Where(e => e.QuestionId != null))
            {
                var tally = Tally(e.QuestionId!);
                if (e.Step is { } step)
                    tally.Step = Math.Max(tally.Step, step);

                switch (e.Kind)
                {
                    case SurveyEventKind.QuestionShown:
                        tally.Reached.Add(session.Id);
                        shownAt[e.QuestionId!] = e.OccurredAt;
                        break;
                    case SurveyEventKind.Answered or SurveyEventKind.Skipped:
                        (e.Kind == SurveyEventKind.Answered ? tally.Answered : tally.Skipped).Add(session.Id);
                        if (shownAt.TryGetValue(e.QuestionId!, out var shown)
                            && e.OccurredAt - shown is var spent && spent >= TimeSpan.Zero && spent <= MaxThinkingTime)
                            tally.Seconds.Add(spent.TotalSeconds);
                        break;
                    case SurveyEventKind.InvalidInput:
                        tally.InvalidInputs++;
                        break;
                    case SurveyEventKind.Back:
                        tally.Backs++;
                        break;
                }
            }

            if (session.State == SessionState.Abandoned && session.LastQuestionId is { } last)
                Tally(last).QuitHere++;
        }

        return stats
            .Select(pair => new QuestionStatDto(
                pair.Key,
                pair.Value.Step,
                pair.Value.Reached.Count,
                pair.Value.Answered.Count,
                pair.Value.Skipped.Count,
                pair.Value.QuitHere,
                Median(pair.Value.Seconds),
                pair.Value.InvalidInputs,
                pair.Value.Backs))
            .OrderBy(q => q.Step)
            .ToList();
    }

    private static int[] StartsByHour(IEnumerable<Session> sessions, TimeZoneInfo zone)
    {
        var hours = new int[24];
        foreach (var session in sessions)
            if (session.SurveyStartedAt is { } started)
                hours[TimeZoneInfo.ConvertTimeFromUtc(started, zone).Hour]++;
        return hours;
    }

    private static double? Median(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        if (sorted.Count == 0)
            return null;
        var middle = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
    }

    private sealed class QuestionTally
    {
        public int Step;
        public readonly HashSet<string> Reached = new();
        public readonly HashSet<string> Answered = new();
        public readonly HashSet<string> Skipped = new();
        public readonly List<double> Seconds = new();
        public int QuitHere;
        public int InvalidInputs;
        public int Backs;
    }

    private enum SessionState
    {
        /// <summary>Only pressed /start, never began the survey.</summary>
        NotStarted,
        InProgress,
        Completed,
        Abandoned
    }

    private sealed class Session
    {
        public required string Id { get; init; }
        public required long TelegramId { get; init; }
        public required IReadOnlyList<SurveyEvent> Events { get; init; }
        public required DateTime StartedAt { get; init; }
        public DateTime? SurveyStartedAt { get; init; }
        public DateTime? CompletedAt { get; init; }
        public DateTime? RemindedAt { get; init; }
        public required DateTime LastActivityAt { get; init; }
        public string? Outcome { get; init; }
        public required string SourceKey { get; init; }
        public string? LastQuestionId { get; init; }
        public int? LastStep { get; init; }
        public required SessionState State { get; init; }
        public bool Cancelled { get; init; }

        public bool IsCompleted => CompletedAt != null;

        public bool Has(SurveyEventKind kind) => Events.Any(e => e.Kind == kind);

        public static Session From(IReadOnlyList<SurveyEvent> events, DateTime nowUtc)
        {
            DateTime? First(SurveyEventKind kind) => events.FirstOrDefault(e => e.Kind == kind)?.OccurredAt;

            var completed = events.LastOrDefault(e => e.Kind == SurveyEventKind.Completed);
            var surveyStarted = First(SurveyEventKind.SurveyStarted);
            var lastShown = events.LastOrDefault(e => e.Kind == SurveyEventKind.QuestionShown);
            var cancelled = events.Any(e => e.Kind == SurveyEventKind.Cancelled);

            // A reminder is the bot talking, not the person: it must not keep a dead survey "in progress"
            var lastActivity = events.LastOrDefault(e => e.Kind != SurveyEventKind.ReminderSent)?.OccurredAt
                               ?? events[0].OccurredAt;

            var state = completed != null ? SessionState.Completed
                : surveyStarted == null ? SessionState.NotStarted
                : cancelled || nowUtc - lastActivity >= AbandonAfter ? SessionState.Abandoned
                : SessionState.InProgress;

            return new Session
            {
                Id = events[0].SessionId,
                TelegramId = events[0].TelegramId,
                Events = events,
                StartedAt = events[0].OccurredAt,
                SurveyStartedAt = surveyStarted,
                CompletedAt = completed?.OccurredAt,
                RemindedAt = First(SurveyEventKind.ReminderSent),
                LastActivityAt = lastActivity,
                Outcome = completed?.Detail,
                // Without a deep link the bot learns the source only from the last question
                SourceKey = events.LastOrDefault(e => e.Source != null)?.Source ?? UnknownKey,
                LastQuestionId = lastShown?.QuestionId,
                LastStep = lastShown?.Step,
                State = state,
                Cancelled = cancelled
            };
        }

        public AbandonedSessionDto ToAbandoned()
        {
            var person = Events.LastOrDefault(e => e.TelegramUsername != null || e.TelegramName != null) ?? Events[0];
            return new AbandonedSessionDto(
                Id,
                Events[0].TelegramId,
                person.TelegramUsername,
                person.TelegramName,
                SourceKey == UnknownKey ? null : SourceKey,
                LastQuestionId,
                LastStep,
                LastActivityAt,
                Cancelled,
                RemindedAt != null);
        }
    }
}
