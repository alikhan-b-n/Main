using Lama.Domain.Scheduling.Entities;

namespace Lama.Domain.Scheduling;

/// <summary>A stretch the consultant is not free: another calendar event or a booked call.</summary>
public record BusyInterval(DateTime StartUtc, DateTime EndUtc);

/// <summary>
/// One bookable time. Busy ones are kept in the list on purpose: the bot shows them
/// greyed out, so a person sees the day is filling up rather than an empty screen.
/// </summary>
public record CallSlot(DateTime StartUtc, DateTime LocalStart, bool IsFree);

public record SlotDay(DateOnly Date, IReadOnlyList<CallSlot> Slots)
{
    public int FreeCount => Slots.Count(s => s.IsFree);
    public bool HasFree => FreeCount > 0;
}

/// <summary>
/// Turns the schedule plus the consultant's busy time into the slots a person may pick.
/// Pure and side-effect free: everything it needs is passed in, which is what makes the
/// rules (lead time, horizon, daylight saving) testable without a calendar.
/// </summary>
public static class SlotPlanner
{
    /// <summary>All open days, or just <paramref name="onlyDay"/> when one is asked for.</summary>
    public static IReadOnlyList<SlotDay> Plan(
        AvailabilitySettings settings,
        IEnumerable<BusyInterval> busy,
        DateTimeOffset now,
        DateOnly? onlyDay = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.Enabled)
            return Array.Empty<SlotDay>();

        var zone = settings.TimeZone();
        var nowUtc = now.UtcDateTime;
        var earliestUtc = nowUtc.AddHours(settings.MinLeadTimeHours);

        var firstDay = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, zone));
        var lastDay = firstDay.AddDays(settings.MaxDaysAhead);

        if (onlyDay is { } requested && (requested < firstDay || requested > lastDay))
            return Array.Empty<SlotDay>();

        var blocked = Expand(busy, settings.BufferMinutes);
        var days = new List<SlotDay>();

        for (var date = onlyDay ?? firstDay; date <= (onlyDay ?? lastDay); date = date.AddDays(1))
        {
            var slots = SlotsOn(date, settings, zone, blocked, earliestUtc);
            if (slots.Count > 0)
                days.Add(new SlotDay(date, slots));
        }

        return days;
    }

    /// <summary>
    /// Whether a booking request still hits a free slot. Deliberately runs the same code
    /// as <see cref="Plan"/>: a time nobody could have seen must not be bookable either.
    /// </summary>
    public static bool IsBookable(
        AvailabilitySettings settings,
        IEnumerable<BusyInterval> busy,
        DateTimeOffset now,
        DateTime startUtc)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (startUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Start must be in UTC", nameof(startUtc));

        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(startUtc, settings.TimeZone()));
        return Plan(settings, busy, now, localDate)
            .SelectMany(day => day.Slots)
            .Any(slot => slot.IsFree && slot.StartUtc == startUtc);
    }

    private static List<CallSlot> SlotsOn(
        DateOnly date,
        AvailabilitySettings settings,
        TimeZoneInfo zone,
        IReadOnlyList<BusyInterval> blocked,
        DateTime earliestUtc)
    {
        var slots = new List<CallSlot>();
        var length = TimeSpan.FromMinutes(settings.SlotMinutes);

        foreach (var window in settings.WindowsOn(date.DayOfWeek))
        {
            var windowEnd = date.ToDateTime(window.End);

            for (var local = date.ToDateTime(window.Start); local + length <= windowEnd; local += length)
            {
                // The clock skipped this hour (spring forward): the time does not exist
                if (zone.IsInvalidTime(local))
                    continue;

                var startUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), zone);
                if (startUtc < earliestUtc)
                    continue;

                var endUtc = startUtc + length;
                var free = !blocked.Any(b => b.StartUtc < endUtc && startUtc < b.EndUtc);
                slots.Add(new CallSlot(startUtc, local, free));
            }
        }

        return slots.OrderBy(s => s.StartUtc).ToList();
    }

    private static List<BusyInterval> Expand(IEnumerable<BusyInterval>? busy, int bufferMinutes)
    {
        var buffer = TimeSpan.FromMinutes(bufferMinutes);
        return (busy ?? Array.Empty<BusyInterval>())
            .Where(b => b.EndUtc > b.StartUtc)
            .Select(b => new BusyInterval(b.StartUtc - buffer, b.EndUtc + buffer))
            .ToList();
    }
}
