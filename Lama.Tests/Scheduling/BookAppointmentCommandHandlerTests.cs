using Lama.Application.Scheduling;
using Lama.Application.Scheduling.Commands;
using Lama.Domain.LeadManagement.Entities;
using Lama.Domain.Scheduling;
using Lama.Domain.Scheduling.Entities;
using Lama.Tests.LeadManagement;
using Microsoft.Extensions.Time.Testing;

namespace Lama.Tests.Scheduling;

public class BookAppointmentCommandHandlerTests
{
    // Wednesday 1 October 2025, 09:00 London
    private static readonly DateTimeOffset Now = new(2025, 10, 1, 8, 0, 0, TimeSpan.Zero);

    private readonly InMemorySettingsRepository _settings = new();
    private readonly InMemoryAppointmentRepository _appointments = new();
    private readonly FakeCalendar _calendar = new();
    private readonly FakeLeadRepository _leads = new();
    private readonly BookAppointmentCommandHandler _handler;

    public BookAppointmentCommandHandlerTests()
    {
        _handler = new BookAppointmentCommandHandler(
            _settings,
            _appointments,
            _calendar,
            _leads,
            new BusyTimeReader(_calendar, _appointments),
            new FakeTimeProvider(Now));
    }

    /// <summary>Thursday 2 October, 15:00 London — a slot the default schedule offers.</summary>
    private static DateTime Slot(int hour = 15, int minute = 0, int day = 2) =>
        TimeZoneInfo.ConvertTimeToUtc(
            new DateTime(2025, 10, day, hour, minute, 0, DateTimeKind.Unspecified),
            TimeZoneInfo.FindSystemTimeZoneById("Europe/London"));

    private Task<BookAppointmentResult> Book(DateTime? startUtc = null, Guid? leadId = null) =>
        _handler.Handle(new BookAppointmentCommand(
            leadId, "Dana Serik", "dana@example.kz", startUtc ?? Slot(),
            Phone: "+7 700 000 00 00", TelegramUsername: "dana", TelegramId: 42), CancellationToken.None);

    [Fact]
    public async Task AFreeSlotIsBookedAndTheMeetLinkComesBack()
    {
        var result = await Book();

        var appointment = Assert.Single(_appointments.Appointments);
        Assert.Equal(AppointmentStatus.Scheduled, appointment.Status);
        Assert.Equal(Slot(), appointment.StartsAtUtc);
        Assert.Equal(30, appointment.DurationMinutes);
        Assert.Equal("evt-1", appointment.GoogleEventId);
        Assert.Equal(result.MeetUrl, appointment.MeetUrl);
        Assert.StartsWith("https://meet.google.com/", result.MeetUrl);
        Assert.Equal("02.10.2025 15:00", result.LocalTime);
    }

    [Fact]
    public async Task TheCalendarEventCarriesTheContactDetails()
    {
        await Book();

        var request = Assert.Single(_calendar.Created);
        Assert.Contains("Dana Serik", request.Summary);
        Assert.Contains("dana@example.kz", request.Description);
        Assert.Contains("+7 700 000 00 00", request.Description);
        Assert.Contains("@dana", request.Description);
        Assert.Equal("dana@example.kz", request.AttendeeEmail);
        Assert.Equal(30, request.DurationMinutes);
    }

    [Fact]
    public async Task ASlotTakenInGoogleIsRefused()
    {
        _calendar.Busy.Add(new BusyInterval(Slot(), Slot().AddMinutes(30)));

        await Assert.ThrowsAsync<SlotUnavailableException>(() => Book());
        Assert.Empty(_appointments.Appointments);
    }

    [Fact]
    public async Task TheSameSlotCannotBeBookedTwice()
    {
        await Book();

        await Assert.ThrowsAsync<SlotUnavailableException>(() => Book());
        Assert.Single(_appointments.Appointments);
    }

    [Fact]
    public async Task ATimeOutsideWorkingHoursIsRefused()
    {
        await Assert.ThrowsAsync<SlotUnavailableException>(() => Book(Slot(20)));
    }

    [Fact]
    public async Task ATimeInsideTheLeadTimeIsRefused()
    {
        // Wednesday itself is closer than the 24 hours the schedule requires
        await Assert.ThrowsAsync<SlotUnavailableException>(() => Book(Slot(hour: 10, day: 1)));
    }

    [Fact]
    public async Task BookingIsRefusedWhileItIsSwitchedOff()
    {
        _settings.Settings = AvailabilitySettings.Create(
            false, "Europe/London", 30, 24, 14, 0, Array.Empty<WorkingWindow>());

        await Assert.ThrowsAsync<BookingDisabledException>(() => Book());
        Assert.Empty(_calendar.Created);
    }

    [Fact]
    public async Task NothingIsStoredWhenGoogleFails()
    {
        _calendar.Failure = new CalendarUnavailableException("network is down");

        await Assert.ThrowsAsync<CalendarUnavailableException>(() => Book());
        Assert.Empty(_appointments.Appointments);
    }

    [Fact]
    public async Task WithoutCredentialsTheCallIsStillBookedLocally()
    {
        _calendar.IsConfigured = false;

        var result = await Book();

        Assert.Null(result.MeetUrl);
        Assert.Empty(_calendar.Created);
        Assert.Single(_appointments.Appointments);
    }

    [Fact]
    public async Task BookingAgainReplacesTheEarlierCall()
    {
        var lead = Lead.Create(LeadSamples.Submission());
        _leads.Leads.Add(lead);

        var first = await Book(leadId: lead.Id);
        var second = await Book(Slot(16), lead.Id);

        Assert.NotEqual(first.AppointmentId, second.AppointmentId);
        Assert.Equal(2, _appointments.Appointments.Count);
        Assert.Equal(AppointmentStatus.Cancelled,
            _appointments.Appointments.Single(a => a.Id == first.AppointmentId).Status);
        Assert.Contains("evt-1", _calendar.Cancelled);
    }

    [Fact]
    public async Task TheLeadMovesToScheduledAndGetsANote()
    {
        var lead = Lead.Create(LeadSamples.Submission());
        _leads.Leads.Add(lead);

        await Book(leadId: lead.Id);

        Assert.Equal(LeadStatus.ConsultationScheduled, lead.Status);
        Assert.Contains(lead.Events, e => e.Text != null && e.Text.Contains("02.10.2025 15:00"));
    }

    [Fact]
    public async Task ALeadThatIsAlreadyLostKeepsItsStatus()
    {
        var lead = Lead.Create(LeadSamples.Submission());
        lead.ChangeStatus(LeadStatus.Lost, "changed their mind");
        _leads.Leads.Add(lead);

        await Book(leadId: lead.Id);

        Assert.Equal(LeadStatus.Lost, lead.Status);
        Assert.Single(_appointments.Appointments);
    }

    [Fact]
    public async Task BookingWithoutALeadWorks()
    {
        var result = await Book(leadId: null);

        Assert.NotEqual(Guid.Empty, result.AppointmentId);
        Assert.Null(Assert.Single(_appointments.Appointments).LeadId);
        Assert.Equal(0, _leads.SaveCount);
    }
}
