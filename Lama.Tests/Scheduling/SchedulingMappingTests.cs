using Lama.Application.Scheduling;
using Lama.Domain.Scheduling;
using Lama.Domain.Scheduling.Entities;

namespace Lama.Tests.Scheduling;

public class SchedulingMappingTests
{
    [Fact]
    public void AfternoonSlotsKeepTheirTwentyFourHourTime()
    {
        var slot = new CallSlot(
            new DateTime(2025, 10, 2, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 10, 2, 15, 0, 0),
            IsFree: true);

        Assert.Equal("15:00", slot.ToDto().LocalTime);
    }

    [Fact]
    public void WorkingHoursAreWrittenInTwentyFourHourTime()
    {
        var settings = AvailabilitySettings.Default().ToDto();

        Assert.Contains(settings.Windows, w => w is { Start: "15:00", End: "17:00" });
        Assert.Contains(settings.Windows, w => w is { Start: "10:00", End: "12:00" });
    }

    [Fact]
    public void ABookedCallIsShownInTheZoneItWasBookedIn()
    {
        var appointment = Appointment.Book(
            null, "Dana", "dana@example.kz",
            new DateTime(2025, 10, 2, 14, 0, 0, DateTimeKind.Utc), 30, "Europe/London");

        var dto = appointment.ToDto();

        Assert.Equal("2025-10-02 15:00", dto.LocalTime);
        Assert.Equal(DateTimeKind.Utc, dto.StartsAtUtc.Kind);
        Assert.Equal("Scheduled", dto.Status);
        Assert.Equal("IntroCall", dto.Kind);
    }

    [Fact]
    public void AnUnknownStoredZoneFallsBackToUtcInsteadOfFailing()
    {
        var appointment = Appointment.Book(
            null, "Dana", "dana@example.kz",
            new DateTime(2025, 10, 2, 14, 0, 0, DateTimeKind.Utc), 30, "Mars/Olympus");

        Assert.Equal("2025-10-02 14:00", appointment.ToDto().LocalTime);
    }

    [Theory]
    [InlineData("09:30", 9, 30)]
    [InlineData("15:00", 15, 0)]
    public void TimesAreParsedFromTheSameFormatTheyArePrintedIn(string value, int hour, int minute)
    {
        Assert.Equal(new TimeOnly(hour, minute), SchedulingTime.ParseTime(value, "test"));
    }

    [Theory]
    [InlineData("3:00 PM")]
    [InlineData("25:00")]
    [InlineData("")]
    public void AnUnparsableTimeIsRejected(string value)
    {
        Assert.Throws<ArgumentException>(() => SchedulingTime.ParseTime(value, "test"));
    }
}
