using Lama.Domain.Common;

namespace Lama.Domain.Scheduling.Entities;

/// <summary>
/// When the intro call can be booked. A single row, edited by admins in the CRM:
/// the bot never decides this on its own, it only asks for the slots this produces.
/// </summary>
public class AvailabilitySettings : AggregateRoot
{
    /// <summary>Fixed id: there is exactly one settings row.</summary>
    public static readonly Guid SingletonId = new("9f1d2c4e-0000-4000-8000-000000000001");

    public const int MinSlotMinutes = 15;
    public const int MaxSlotMinutes = 240;
    public const int MaxDaysAheadLimit = 90;
    public const int MaxWindows = 40;

    /// <summary>Off means the bot falls back to "a manager will contact you".</summary>
    public bool Enabled { get; private set; }

    /// <summary>IANA id, e.g. "Europe/London". Working hours below are in this zone.</summary>
    public string TimeZoneId { get; private set; }

    public int SlotMinutes { get; private set; }

    /// <summary>How soon the earliest slot may start, counted from now.</summary>
    public int MinLeadTimeHours { get; private set; }

    /// <summary>How far ahead the calendar is open.</summary>
    public int MaxDaysAhead { get; private set; }

    /// <summary>Free time kept around someone else's event, so calls do not touch it.</summary>
    public int BufferMinutes { get; private set; }

    private readonly List<WorkingWindow> _windows = new();
    public IReadOnlyList<WorkingWindow> Windows => _windows.AsReadOnly();

    private AvailabilitySettings()
    {
        TimeZoneId = null!;
    }

    /// <summary>The schedule IconicU starts with; admins change it in the CRM.</summary>
    public static AvailabilitySettings Default()
    {
        var windows = new[] { DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Friday }
            .SelectMany(day => new[]
            {
                new WorkingWindow(day, new TimeOnly(10, 0), new TimeOnly(12, 0)),
                new WorkingWindow(day, new TimeOnly(15, 0), new TimeOnly(17, 0))
            });

        return Create(
            enabled: true,
            timeZoneId: "Europe/London",
            slotMinutes: 30,
            minLeadTimeHours: 24,
            maxDaysAhead: 14,
            bufferMinutes: 0,
            windows: windows);
    }

    public static AvailabilitySettings Create(
        bool enabled,
        string timeZoneId,
        int slotMinutes,
        int minLeadTimeHours,
        int maxDaysAhead,
        int bufferMinutes,
        IEnumerable<WorkingWindow> windows)
    {
        var settings = new AvailabilitySettings { Id = SingletonId };
        settings.Update(enabled, timeZoneId, slotMinutes, minLeadTimeHours, maxDaysAhead, bufferMinutes, windows);
        return settings;
    }

    public void Update(
        bool enabled,
        string timeZoneId,
        int slotMinutes,
        int minLeadTimeHours,
        int maxDaysAhead,
        int bufferMinutes,
        IEnumerable<WorkingWindow> windows)
    {
        // Resolved here so a bad zone is rejected before anything is stored
        ResolveTimeZone(timeZoneId);

        if (slotMinutes is < MinSlotMinutes or > MaxSlotMinutes)
            throw new ArgumentOutOfRangeException(nameof(slotMinutes),
                $"Slot length must be between {MinSlotMinutes} and {MaxSlotMinutes} minutes");
        if (minLeadTimeHours is < 0 or > 24 * 30)
            throw new ArgumentOutOfRangeException(nameof(minLeadTimeHours),
                "Lead time must be between 0 hours and 30 days");
        if (maxDaysAhead is < 1 or > MaxDaysAheadLimit)
            throw new ArgumentOutOfRangeException(nameof(maxDaysAhead),
                $"The calendar may be open for 1 to {MaxDaysAheadLimit} days");
        if (bufferMinutes is < 0 or > MaxSlotMinutes)
            throw new ArgumentOutOfRangeException(nameof(bufferMinutes),
                $"Buffer must be between 0 and {MaxSlotMinutes} minutes");

        var cleaned = Normalize(windows, slotMinutes);
        if (enabled && cleaned.Count == 0)
            throw new ArgumentException("At least one working window is required while booking is on", nameof(windows));

        Enabled = enabled;
        TimeZoneId = timeZoneId.Trim();
        SlotMinutes = slotMinutes;
        MinLeadTimeHours = minLeadTimeHours;
        MaxDaysAhead = maxDaysAhead;
        BufferMinutes = bufferMinutes;

        _windows.Clear();
        _windows.AddRange(cleaned);
        UpdatedAt = DateTime.UtcNow;
    }

    public TimeZoneInfo TimeZone() => ResolveTimeZone(TimeZoneId);

    public IEnumerable<WorkingWindow> WindowsOn(DayOfWeek day) =>
        _windows.Where(w => w.Day == day).OrderBy(w => w.Start);

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ArgumentException("Time zone is required", nameof(timeZoneId));
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new ArgumentException($"Unknown time zone '{timeZoneId}'", nameof(timeZoneId), ex);
        }
    }

    /// <summary>Drops windows too short for a single slot and rejects overlaps on the same day.</summary>
    private static List<WorkingWindow> Normalize(IEnumerable<WorkingWindow> windows, int slotMinutes)
    {
        var ordered = (windows ?? throw new ArgumentNullException(nameof(windows)))
            .Where(w => w.Length >= TimeSpan.FromMinutes(slotMinutes))
            .Distinct()
            .OrderBy(w => w.Day).ThenBy(w => w.Start)
            .ToList();

        if (ordered.Count > MaxWindows)
            throw new ArgumentException($"No more than {MaxWindows} working windows are supported", nameof(windows));

        for (var i = 1; i < ordered.Count; i++)
        {
            var previous = ordered[i - 1];
            var current = ordered[i];
            if (current.Day == previous.Day && current.Start < previous.End)
                throw new ArgumentException(
                    $"Working windows on {current.Day} overlap: {previous} and {current}", nameof(windows));
        }

        return ordered;
    }
}

/// <summary>A stretch of a weekday when calls are taken, in the settings' time zone.</summary>
public record WorkingWindow
{
    public DayOfWeek Day { get; }
    public TimeOnly Start { get; }
    public TimeOnly End { get; }

    public WorkingWindow(DayOfWeek day, TimeOnly start, TimeOnly end)
    {
        if (!Enum.IsDefined(day))
            throw new ArgumentOutOfRangeException(nameof(day));
        if (end <= start)
            throw new ArgumentException($"A working window must end after it starts ({start}–{end})", nameof(end));

        Day = day;
        Start = start;
        End = end;
    }

    public TimeSpan Length => End - Start;

    // Verbatim: the colon has to be escaped inside a TimeOnly format string
    private const string HourFormat = @"HH\:mm";

    public override string ToString() => $"{Day} {Start.ToString(HourFormat)}–{End.ToString(HourFormat)}";
}
