using Lama.Domain.Scheduling;
using Lama.Domain.Scheduling.Entities;

namespace Lama.Tests.Scheduling;

public class SlotPlannerTests
{
    private static readonly TimeZoneInfo London = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

    // Wednesday 1 October 2025, 09:00 London (08:00 UTC, summer time)
    private static readonly DateTimeOffset Now = new(2025, 10, 1, 8, 0, 0, TimeSpan.Zero);

    private static AvailabilitySettings Settings(
        int slotMinutes = 30,
        int minLeadTimeHours = 24,
        int maxDaysAhead = 14,
        int bufferMinutes = 0,
        bool enabled = true,
        IEnumerable<WorkingWindow>? windows = null)
        => AvailabilitySettings.Create(
            enabled,
            "Europe/London",
            slotMinutes,
            minLeadTimeHours,
            maxDaysAhead,
            bufferMinutes,
            windows ?? new[]
            {
                new WorkingWindow(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(12, 0)),
                new WorkingWindow(DayOfWeek.Thursday, new TimeOnly(10, 0), new TimeOnly(12, 0)),
                new WorkingWindow(DayOfWeek.Thursday, new TimeOnly(15, 0), new TimeOnly(17, 0)),
                new WorkingWindow(DayOfWeek.Friday, new TimeOnly(10, 0), new TimeOnly(12, 0))
            });

    private static DateTime Utc(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    /// <summary>A London wall-clock time as the instant it actually happens.</summary>
    private static DateTime LondonTime(int year, int month, int day, int hour, int minute) =>
        TimeZoneInfo.ConvertTimeToUtc(new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified), London);

    private static CallSlot At(SlotDay day, int hour, int minute) =>
        day.Slots.Single(s => TimeOnly.FromDateTime(s.LocalStart) == new TimeOnly(hour, minute));

    [Fact]
    public void Plan_OffersOnlyTheConfiguredWeekdays()
    {
        var days = SlotPlanner.Plan(Settings(), Array.Empty<BusyInterval>(), Now);

        Assert.NotEmpty(days);
        Assert.All(days, day => Assert.Contains(day.Date.DayOfWeek,
            new[] { DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Friday }));
    }

    [Fact]
    public void Plan_FillsEachWindowWithWholeSlots()
    {
        var days = SlotPlanner.Plan(Settings(minLeadTimeHours: 0), Array.Empty<BusyInterval>(), Now);
        var thursday = days.Single(d => d.Date == new DateOnly(2025, 10, 2));

        // Two two-hour windows, half-hour slots
        Assert.Equal(8, thursday.Slots.Count);
        Assert.Equal(new TimeOnly(10, 0), TimeOnly.FromDateTime(thursday.Slots[0].LocalStart));
        Assert.Equal(new TimeOnly(16, 30), TimeOnly.FromDateTime(thursday.Slots[^1].LocalStart));
    }

    [Fact]
    public void Plan_LeavesOutSlotsInsideTheLeadTime()
    {
        // 30 hours from Wednesday 09:00 London lands on Thursday 15:00: the morning is too close
        var days = SlotPlanner.Plan(Settings(minLeadTimeHours: 30), Array.Empty<BusyInterval>(), Now);
        var thursday = days.Single(d => d.Date == new DateOnly(2025, 10, 2));

        Assert.DoesNotContain(thursday.Slots, s => s.StartUtc < Now.UtcDateTime.AddHours(30));
        Assert.Equal(4, thursday.Slots.Count); // only the 15:00-17:00 window survives
        Assert.Equal(new TimeOnly(15, 0), TimeOnly.FromDateTime(thursday.Slots[0].LocalStart));
    }

    [Fact]
    public void Plan_StopsAtTheHorizon()
    {
        var days = SlotPlanner.Plan(Settings(maxDaysAhead: 7), Array.Empty<BusyInterval>(), Now);

        Assert.All(days, day => Assert.True(day.Date <= new DateOnly(2025, 10, 8)));
    }

    [Fact]
    public void Plan_KeepsBusySlotsInTheListButMarksThem()
    {
        var busy = new[] { new BusyInterval(LondonTime(2025, 10, 2, 15, 0), LondonTime(2025, 10, 2, 16, 0)) };

        var thursday = SlotPlanner.Plan(Settings(), busy, Now).Single(d => d.Date == new DateOnly(2025, 10, 2));

        Assert.Equal(8, thursday.Slots.Count);
        Assert.Equal(6, thursday.FreeCount);
        Assert.False(At(thursday, 15, 0).IsFree);
        Assert.False(At(thursday, 15, 30).IsFree);
        Assert.True(At(thursday, 16, 0).IsFree);
    }

    [Fact]
    public void Plan_TreatsTouchingIntervalsAsFree()
    {
        // An event ending exactly when the slot starts does not block it
        var busy = new[] { new BusyInterval(LondonTime(2025, 10, 2, 14, 0), LondonTime(2025, 10, 2, 15, 0)) };

        var thursday = SlotPlanner.Plan(Settings(), busy, Now).Single(d => d.Date == new DateOnly(2025, 10, 2));

        Assert.True(At(thursday, 15, 0).IsFree);
    }

    [Fact]
    public void Plan_KeepsTheBufferAroundOtherEvents()
    {
        var busy = new[] { new BusyInterval(LondonTime(2025, 10, 2, 14, 0), LondonTime(2025, 10, 2, 15, 0)) };

        var thursday = SlotPlanner.Plan(Settings(bufferMinutes: 15), busy, Now)
            .Single(d => d.Date == new DateOnly(2025, 10, 2));

        Assert.False(At(thursday, 15, 0).IsFree); // now falls inside the buffer
        Assert.True(At(thursday, 15, 30).IsFree);
    }

    [Fact]
    public void Plan_ReturnsNothingWhenBookingIsOff()
    {
        Assert.Empty(SlotPlanner.Plan(Settings(enabled: false), Array.Empty<BusyInterval>(), Now));
    }

    [Fact]
    public void Plan_ForASingleDayReturnsThatDayOnly()
    {
        var days = SlotPlanner.Plan(Settings(), Array.Empty<BusyInterval>(), Now, new DateOnly(2025, 10, 3));

        Assert.Equal(new DateOnly(2025, 10, 3), Assert.Single(days).Date);
    }

    [Fact]
    public void Plan_ForADayBeyondTheHorizonReturnsNothing()
    {
        Assert.Empty(SlotPlanner.Plan(Settings(maxDaysAhead: 7), Array.Empty<BusyInterval>(), Now,
            new DateOnly(2025, 12, 1)));
    }

    [Fact]
    public void Plan_HoldsTheLocalHourAcrossTheClockChange()
    {
        // Britain leaves summer time on 26 October 2025: a Monday slot after that date
        // must still start at 10:00 in London, which is a different hour in UTC.
        var settings = Settings(minLeadTimeHours: 0, maxDaysAhead: 40);
        var days = SlotPlanner.Plan(settings, Array.Empty<BusyInterval>(), Now);

        var before = days.Single(d => d.Date == new DateOnly(2025, 10, 20)).Slots[0];
        var after = days.Single(d => d.Date == new DateOnly(2025, 11, 3)).Slots[0];

        Assert.Equal(new TimeOnly(10, 0), TimeOnly.FromDateTime(before.LocalStart));
        Assert.Equal(new TimeOnly(10, 0), TimeOnly.FromDateTime(after.LocalStart));
        Assert.Equal(Utc(2025, 10, 20, 9, 0), before.StartUtc);
        Assert.Equal(Utc(2025, 11, 3, 10, 0), after.StartUtc);
    }

    [Fact]
    public void IsBookable_AcceptsAFreeSlotAndRejectsEverythingElse()
    {
        var settings = Settings();
        var free = LondonTime(2025, 10, 2, 15, 0);

        Assert.True(SlotPlanner.IsBookable(settings, Array.Empty<BusyInterval>(), Now, free));
        // Taken
        Assert.False(SlotPlanner.IsBookable(settings,
            new[] { new BusyInterval(free, free.AddMinutes(30)) }, Now, free));
        // Not on the grid
        Assert.False(SlotPlanner.IsBookable(settings, Array.Empty<BusyInterval>(), Now,
            LondonTime(2025, 10, 2, 15, 10)));
        // Outside the working windows
        Assert.False(SlotPlanner.IsBookable(settings, Array.Empty<BusyInterval>(), Now,
            LondonTime(2025, 10, 2, 20, 0)));
        // Inside the lead time: with 30 hours of notice Thursday morning is out of reach
        Assert.False(SlotPlanner.IsBookable(Settings(minLeadTimeHours: 30), Array.Empty<BusyInterval>(), Now,
            LondonTime(2025, 10, 2, 10, 0)));
    }

    [Fact]
    public void IsBookable_RejectsANonUtcTime()
    {
        Assert.Throws<ArgumentException>(() => SlotPlanner.IsBookable(
            Settings(), Array.Empty<BusyInterval>(), Now, new DateTime(2025, 10, 2, 15, 0, 0)));
    }
}
