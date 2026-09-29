using Lama.Domain.Scheduling.Entities;

namespace Lama.Tests.Scheduling;

public class AppointmentTests
{
    private static readonly DateTime Start = new(2025, 10, 2, 14, 0, 0, DateTimeKind.Utc);

    private static Appointment Book(
        DateTime? startsAtUtc = null,
        int durationMinutes = 30,
        string fullName = "Aigerim Nurlanovna",
        string email = "student@example.kz")
        => Appointment.Book(
            leadId: Guid.NewGuid(),
            fullName: fullName,
            email: email,
            startsAtUtc: startsAtUtc ?? Start,
            durationMinutes: durationMinutes,
            timeZoneId: "Europe/London");

    [Fact]
    public void ABookedCallStartsOutScheduled()
    {
        var appointment = Book();

        Assert.Equal(AppointmentStatus.Scheduled, appointment.Status);
        Assert.Equal(AppointmentKind.IntroCall, appointment.Kind);
        Assert.True(appointment.IsActive);
        Assert.Equal(Start.AddMinutes(30), appointment.EndsAtUtc);
        Assert.Null(appointment.GoogleEventId);
    }

    [Fact]
    public void ContactDetailsAreTrimmed()
    {
        var appointment = Appointment.Book(null, "  Dana  ", " dana@example.kz ", Start, 30, " Europe/London ",
            phone: "  +7 700 000 00 00 ", telegramUsername: "  dana ", comment: "  first call  ");

        Assert.Equal("Dana", appointment.FullName);
        Assert.Equal("dana@example.kz", appointment.Email.Value);
        Assert.Equal("+7 700 000 00 00", appointment.Phone);
        Assert.Equal("dana", appointment.TelegramUsername);
        Assert.Equal("Europe/London", appointment.TimeZoneId);
        Assert.Equal("first call", appointment.Comment);
    }

    [Fact]
    public void BlankOptionalDetailsBecomeNull()
    {
        var appointment = Appointment.Book(null, "Dana", "dana@example.kz", Start, 30, "Europe/London",
            phone: "   ", telegramUsername: "", comment: " ");

        Assert.Null(appointment.Phone);
        Assert.Null(appointment.TelegramUsername);
        Assert.Null(appointment.Comment);
    }

    [Fact]
    public void ALocalStartIsRejected()
    {
        Assert.Throws<ArgumentException>(() => Book(new DateTime(2025, 10, 2, 14, 0, 0)));
    }

    [Fact]
    public void ANameIsRequired()
    {
        Assert.Throws<ArgumentException>(() => Book(fullName: "   "));
    }

    [Fact]
    public void AnInvalidEmailIsRejected()
    {
        Assert.Throws<ArgumentException>(() => Book(email: "not-an-email"));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(600)]
    public void AnImplausibleDurationIsRejected(int durationMinutes)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Book(durationMinutes: durationMinutes));
    }

    [Fact]
    public void ACommentLongerThanTheLimitIsRejected()
    {
        Assert.Throws<ArgumentException>(() => Appointment.Book(
            null, "Dana", "dana@example.kz", Start, 30, "Europe/London",
            comment: new string('x', Appointment.MaxCommentLength + 1)));
    }

    [Fact]
    public void TheCalendarEventIsStoredOnce_ItExists()
    {
        var appointment = Book();

        appointment.AttachCalendarEvent(" evt-1 ", "https://meet.google.com/abc-defg-hij", "https://calendar.google.com/x");

        Assert.Equal("evt-1", appointment.GoogleEventId);
        Assert.Equal("https://meet.google.com/abc-defg-hij", appointment.MeetUrl);
        Assert.Equal("https://calendar.google.com/x", appointment.CalendarLink);
    }

    [Fact]
    public void AnEmptyCalendarEventIdIsRejected()
    {
        Assert.Throws<ArgumentException>(() => Book().AttachCalendarEvent("  ", null, null));
    }

    [Fact]
    public void CancellingRecordsTheReasonAndTheTime()
    {
        var appointment = Book();

        appointment.Cancel("the student asked to move it");

        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        Assert.False(appointment.IsActive);
        Assert.Equal("the student asked to move it", appointment.CancelReason);
        Assert.NotNull(appointment.CancelledAt);
    }

    [Fact]
    public void RescheduleMovesTheCall()
    {
        var appointment = Book();
        var later = Start.AddDays(3);

        appointment.MoveTo(later, 60);

        Assert.Equal(later, appointment.StartsAtUtc);
        Assert.Equal(60, appointment.DurationMinutes);
        Assert.Equal(AppointmentStatus.Scheduled, appointment.Status);
    }

    [Fact]
    public void ACancelledCallCannotBeChangedAnyMore()
    {
        var appointment = Book();
        appointment.Cancel();

        Assert.Throws<InvalidOperationException>(() => appointment.Cancel());
        Assert.Throws<InvalidOperationException>(() => appointment.MoveTo(Start.AddDays(1), 30));
        Assert.Throws<InvalidOperationException>(() => appointment.MarkCompleted());
        Assert.Throws<InvalidOperationException>(() => appointment.MarkNoShow());
    }

    [Fact]
    public void ACallCanBeClosedAsDoneOrAsANoShow()
    {
        var done = Book();
        done.MarkCompleted();
        Assert.Equal(AppointmentStatus.Completed, done.Status);

        var missed = Book();
        missed.MarkNoShow();
        Assert.Equal(AppointmentStatus.NoShow, missed.Status);
    }
}
