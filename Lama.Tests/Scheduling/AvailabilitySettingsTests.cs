using Lama.Domain.Scheduling.Entities;

namespace Lama.Tests.Scheduling;

public class AvailabilitySettingsTests
{
    private static WorkingWindow Morning(DayOfWeek day = DayOfWeek.Monday) =>
        new(day, new TimeOnly(10, 0), new TimeOnly(12, 0));

    private static AvailabilitySettings Create(
        string timeZoneId = "Europe/London",
        int slotMinutes = 30,
        int minLeadTimeHours = 24,
        int maxDaysAhead = 14,
        int bufferMinutes = 0,
        bool enabled = true,
        params WorkingWindow[] windows)
        => AvailabilitySettings.Create(enabled, timeZoneId, slotMinutes, minLeadTimeHours, maxDaysAhead,
            bufferMinutes, windows.Length > 0 ? windows : new[] { Morning() });

    [Fact]
    public void Default_IsTheScheduleIconicUAgreedOn()
    {
        var settings = AvailabilitySettings.Default();

        Assert.True(settings.Enabled);
        Assert.Equal("Europe/London", settings.TimeZoneId);
        Assert.Equal(30, settings.SlotMinutes);
        Assert.Equal(24, settings.MinLeadTimeHours);
        Assert.Equal(14, settings.MaxDaysAhead);
        Assert.Equal(AvailabilitySettings.SingletonId, settings.Id);

        Assert.Equal(new[] { DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Friday },
            settings.Windows.Select(w => w.Day).Distinct());
        Assert.Equal(2, settings.WindowsOn(DayOfWeek.Monday).Count());
        Assert.Empty(settings.WindowsOn(DayOfWeek.Tuesday));
    }

    [Fact]
    public void WindowsAreOrderedByDayAndTime()
    {
        var settings = Create(windows: new[]
        {
            new WorkingWindow(DayOfWeek.Friday, new TimeOnly(15, 0), new TimeOnly(17, 0)),
            new WorkingWindow(DayOfWeek.Monday, new TimeOnly(15, 0), new TimeOnly(17, 0)),
            new WorkingWindow(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(12, 0))
        });

        Assert.Equal(
            new[] { (DayOfWeek.Monday, 10), (DayOfWeek.Monday, 15), (DayOfWeek.Friday, 15) },
            settings.Windows.Select(w => (w.Day, w.Start.Hour)));
    }

    [Fact]
    public void WindowsShorterThanASlotAreDropped()
    {
        var settings = Create(slotMinutes: 60, windows: new[]
        {
            Morning(),
            new WorkingWindow(DayOfWeek.Friday, new TimeOnly(10, 0), new TimeOnly(10, 30))
        });

        Assert.Equal(DayOfWeek.Monday, Assert.Single(settings.Windows).Day);
    }

    [Fact]
    public void OverlappingWindowsOnTheSameDayAreRejected()
    {
        var overlapping = new[]
        {
            Morning(),
            new WorkingWindow(DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(13, 0))
        };

        var error = Assert.Throws<ArgumentException>(() => Create(windows: overlapping));
        Assert.Contains("overlap", error.Message);
    }

    [Fact]
    public void TheSameWindowOnDifferentDaysIsFine()
    {
        var settings = Create(windows: new[] { Morning(), Morning(DayOfWeek.Friday) });

        Assert.Equal(2, settings.Windows.Count);
    }

    [Fact]
    public void BookingCannotBeOnWithoutWorkingHours()
    {
        Assert.Throws<ArgumentException>(() => AvailabilitySettings.Create(
            true, "Europe/London", 30, 24, 14, 0, Array.Empty<WorkingWindow>()));
    }

    [Fact]
    public void BookingMayBeTurnedOffWithoutWorkingHours()
    {
        var settings = AvailabilitySettings.Create(
            false, "Europe/London", 30, 24, 14, 0, Array.Empty<WorkingWindow>());

        Assert.False(settings.Enabled);
        Assert.Empty(settings.Windows);
    }

    [Theory]
    [InlineData("Europe/London")]
    [InlineData("Asia/Almaty")]
    [InlineData("UTC")]
    public void KnownTimeZonesAreAccepted(string timeZoneId)
    {
        Assert.Equal(timeZoneId, Create(timeZoneId).TimeZoneId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("Mars/Olympus")]
    public void UnknownTimeZonesAreRejected(string timeZoneId)
    {
        Assert.Throws<ArgumentException>(() => Create(timeZoneId));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(300)]
    public void SlotLengthOutsideTheAllowedRangeIsRejected(int slotMinutes)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(slotMinutes: slotMinutes));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(91)]
    public void HorizonOutsideTheAllowedRangeIsRejected(int maxDaysAhead)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(maxDaysAhead: maxDaysAhead));
    }

    [Fact]
    public void NegativeLeadTimeIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(minLeadTimeHours: -1));
    }

    [Fact]
    public void AWindowMustEndAfterItStarts()
    {
        Assert.Throws<ArgumentException>(() =>
            new WorkingWindow(DayOfWeek.Monday, new TimeOnly(12, 0), new TimeOnly(10, 0)));
    }

    [Fact]
    public void UpdateReplacesTheWholeSchedule()
    {
        var settings = AvailabilitySettings.Default();

        settings.Update(true, "Asia/Almaty", 60, 2, 7, 15,
            new[] { new WorkingWindow(DayOfWeek.Saturday, new TimeOnly(9, 0), new TimeOnly(13, 0)) });

        Assert.Equal("Asia/Almaty", settings.TimeZoneId);
        Assert.Equal(60, settings.SlotMinutes);
        Assert.Equal(15, settings.BufferMinutes);
        Assert.Equal(DayOfWeek.Saturday, Assert.Single(settings.Windows).Day);
        Assert.NotNull(settings.UpdatedAt);
    }

    [Fact]
    public void ARejectedUpdateLeavesTheSettingsUntouched()
    {
        var settings = AvailabilitySettings.Default();

        Assert.Throws<ArgumentException>(() => settings.Update(true, "Mars/Olympus", 30, 24, 14, 0, settings.Windows));

        Assert.Equal("Europe/London", settings.TimeZoneId);
        Assert.Equal(6, settings.Windows.Count);
    }
}
