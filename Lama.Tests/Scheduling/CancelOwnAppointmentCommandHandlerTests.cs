using Lama.Application.Scheduling;
using Lama.Application.Scheduling.Commands;
using Lama.Domain.Scheduling.Entities;

namespace Lama.Tests.Scheduling;

public class CancelOwnAppointmentCommandHandlerTests
{
    private const long Owner = 4242;
    private const long Stranger = 777;

    private readonly InMemoryAppointmentRepository _appointments = new();
    private readonly FakeCalendar _calendar = new();
    private readonly CancelOwnAppointmentCommandHandler _handler;

    public CancelOwnAppointmentCommandHandlerTests()
    {
        _handler = new CancelOwnAppointmentCommandHandler(_appointments, _calendar);
    }

    private Appointment Given(long telegramId = Owner, string? eventId = "evt-1")
    {
        var appointment = Appointment.Book(
            null, "Dana", "dana@example.kz",
            new DateTime(2026, 10, 1, 14, 0, 0, DateTimeKind.Utc), 30, "Europe/London",
            telegramId: telegramId);
        if (eventId != null)
            appointment.AttachCalendarEvent(eventId, "https://meet.google.com/x", null);
        _appointments.Appointments.Add(appointment);
        return appointment;
    }

    private Task Cancel(Guid id, long telegramId) =>
        _handler.Handle(new CancelOwnAppointmentCommand(id, telegramId, "передумал"), CancellationToken.None);

    [Fact]
    public async Task OwnCallIsCancelledInTheCalendarToo()
    {
        var appointment = Given();

        await Cancel(appointment.Id, Owner);

        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        Assert.Equal("передумал", appointment.CancelReason);
        Assert.Contains("evt-1", _calendar.Cancelled);
    }

    [Fact]
    public async Task SomebodyElseCannotCancelIt()
    {
        var appointment = Given();

        await Assert.ThrowsAsync<AppointmentNotFoundException>(() => Cancel(appointment.Id, Stranger));

        Assert.Equal(AppointmentStatus.Scheduled, appointment.Status);
        Assert.Empty(_calendar.Cancelled);
    }

    [Fact]
    public async Task AnUnknownIdLooksExactlyLikeSomeoneElsesCall()
    {
        await Assert.ThrowsAsync<AppointmentNotFoundException>(() => Cancel(Guid.NewGuid(), Owner));
    }

    [Fact]
    public async Task CancellingTwiceIsNotAnError()
    {
        var appointment = Given();
        await Cancel(appointment.Id, Owner);

        await Cancel(appointment.Id, Owner);

        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        Assert.Single(_calendar.Cancelled);
    }

    [Fact]
    public async Task ACallWithoutACalendarEventIsStillCancelled()
    {
        var appointment = Given(eventId: null);

        await Cancel(appointment.Id, Owner);

        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        Assert.Empty(_calendar.Cancelled);
    }
}
