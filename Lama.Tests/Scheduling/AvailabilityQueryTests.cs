using Lama.Application.Scheduling;
using Lama.Application.Scheduling.Queries;
using Lama.Domain.Scheduling.Entities;
using Microsoft.Extensions.Time.Testing;

namespace Lama.Tests.Scheduling;

/// <summary>Switching booking off hides the calendar from the bot, never from the managers.</summary>
public class AvailabilityQueryTests
{
    // Wednesday 1 October 2025, 09:00 London
    private static readonly DateTimeOffset Now = new(2025, 10, 1, 8, 0, 0, TimeSpan.Zero);

    private readonly InMemorySettingsRepository _settings = new();
    private readonly FakeCalendar _calendar = new();
    private readonly InMemoryAppointmentRepository _appointments = new();

    public AvailabilityQueryTests()
    {
        var defaults = AvailabilitySettings.Default();
        _settings.Settings = AvailabilitySettings.Create(
            false, defaults.TimeZoneId, defaults.SlotMinutes, defaults.MinLeadTimeHours,
            defaults.MaxDaysAhead, defaults.BufferMinutes, defaults.Windows);
    }

    private Task<AvailabilityDto> Availability(bool forStaff) =>
        new GetAvailabilityQueryHandler(_settings, new BusyTimeReader(_calendar, _appointments), new FakeTimeProvider(Now))
            .Handle(new GetAvailabilityQuery(forStaff), CancellationToken.None);

    private Task<DaySlotsDto> Day(bool forStaff) =>
        new GetDaySlotsQueryHandler(_settings, new BusyTimeReader(_calendar, _appointments), new FakeTimeProvider(Now))
            .Handle(new GetDaySlotsQuery(new DateOnly(2025, 10, 2), forStaff), CancellationToken.None);

    [Fact]
    public async Task TheBotSeesNoDaysWhileBookingIsOff()
    {
        var availability = await Availability(forStaff: false);

        Assert.False(availability.Enabled);
        Assert.Empty(availability.Days);
        Assert.Empty((await Day(forStaff: false)).Slots);
    }

    [Fact]
    public async Task AManagerStillSeesFreeTimeWhileBookingIsOff()
    {
        var availability = await Availability(forStaff: true);

        Assert.False(availability.Enabled);
        Assert.NotEmpty(availability.Days);
        Assert.Contains((await Day(forStaff: true)).Slots, s => s.IsFree);
    }
}
