using Lama.Application.Common;
using Lama.Application.LeadManagement;
using Lama.Domain.LeadManagement.Entities;
using Lama.Domain.Scheduling;
using Lama.Domain.Scheduling.Entities;

namespace Lama.Application.Scheduling.Commands;

/// <summary>Someone picked a time in the bot (or a manager did it for them in the CRM).</summary>
public record BookAppointmentCommand(
    Guid? LeadId,
    string FullName,
    string Email,
    DateTime StartUtc,
    string? Phone = null,
    string? TelegramUsername = null,
    long? TelegramId = null,
    AppointmentKind Kind = AppointmentKind.IntroCall,
    string? Comment = null) : ICommand<BookAppointmentResult>;

public record BookAppointmentResult(
    Guid AppointmentId,
    DateTime StartUtc,
    int DurationMinutes,
    string TimeZoneId,
    string LocalTime,
    string? MeetUrl);

public class BookAppointmentCommandHandler : ICommandHandler<BookAppointmentCommand, BookAppointmentResult>
{
    private readonly IAvailabilitySettingsRepository _settings;
    private readonly IAppointmentRepository _appointments;
    private readonly ISchedulingCalendar _calendar;
    private readonly ILeadRepository _leads;
    private readonly BusyTimeReader _busy;
    private readonly TimeProvider _clock;

    public BookAppointmentCommandHandler(
        IAvailabilitySettingsRepository settings,
        IAppointmentRepository appointments,
        ISchedulingCalendar calendar,
        ILeadRepository leads,
        BusyTimeReader busy,
        TimeProvider clock)
    {
        _settings = settings;
        _appointments = appointments;
        _calendar = calendar;
        _leads = leads;
        _busy = busy;
        _clock = clock;
    }

    public async Task<BookAppointmentResult> Handle(
        BookAppointmentCommand command, CancellationToken cancellationToken)
    {
        var settings = await _settings.GetAsync(cancellationToken);
        if (!settings.Enabled)
            throw new BookingDisabledException();

        var startUtc = SchedulingTime.AsUtc(command.StartUtc);
        var busy = await _busy.ReadAsync(startUtc.AddDays(-1), startUtc.AddDays(1), cancellationToken);

        if (!SlotPlanner.IsBookable(settings, busy, _clock.GetUtcNow(), startUtc))
            throw new SlotUnavailableException(startUtc);

        // Booking again replaces the previous call instead of filling the calendar twice
        if (command.LeadId is { } leadId)
            await CancelPreviousAsync(leadId, cancellationToken);

        var appointment = Appointment.Book(
            command.LeadId,
            command.FullName,
            command.Email,
            startUtc,
            settings.SlotMinutes,
            settings.TimeZoneId,
            command.Kind,
            command.Phone,
            command.TelegramUsername,
            command.TelegramId,
            command.Comment);

        if (_calendar.IsConfigured)
        {
            var created = await _calendar.CreateAsync(
                SchedulingTime.Describe(appointment), cancellationToken);
            appointment.AttachCalendarEvent(created.EventId, created.MeetUrl, created.HtmlLink);
        }

        await _appointments.AddAsync(appointment, cancellationToken);
        await MarkLeadScheduledAsync(appointment, cancellationToken);

        return new BookAppointmentResult(
            appointment.Id,
            appointment.StartsAtUtc,
            appointment.DurationMinutes,
            appointment.TimeZoneId,
            SchedulingTime.Local(appointment),
            appointment.MeetUrl);
    }

    private async Task CancelPreviousAsync(Guid leadId, CancellationToken cancellationToken)
    {
        var previous = await _appointments.FindScheduledForLeadAsync(leadId, cancellationToken);
        if (previous == null)
            return;

        if (previous.GoogleEventId != null && _calendar.IsConfigured)
            await _calendar.CancelAsync(previous.GoogleEventId, cancellationToken);

        previous.Cancel("Заменена новой записью");
        await _appointments.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkLeadScheduledAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        if (appointment.LeadId is not { } leadId)
            return;

        var lead = await _leads.GetByIdAsync(leadId, cancellationToken);
        if (lead == null)
            return;

        lead.AddNote($"Записан на звонок: {SchedulingTime.Local(appointment)} ({appointment.TimeZoneId})");
        if (lead.Status is LeadStatus.New or LeadStatus.Contacted)
            lead.ChangeStatus(LeadStatus.ConsultationScheduled);

        await _leads.SaveChangesAsync(cancellationToken);
    }
}

public record CancelAppointmentCommand(Guid Id, string? Reason = null) : ICommand;

public class CancelAppointmentCommandHandler : ICommandHandler<CancelAppointmentCommand>
{
    private readonly IAppointmentRepository _appointments;
    private readonly ISchedulingCalendar _calendar;

    public CancelAppointmentCommandHandler(IAppointmentRepository appointments, ISchedulingCalendar calendar)
    {
        _appointments = appointments;
        _calendar = calendar;
    }

    public async Task Handle(CancelAppointmentCommand command, CancellationToken cancellationToken)
    {
        var appointment = await _appointments.GetByIdAsync(command.Id, cancellationToken)
                          ?? throw new AppointmentNotFoundException(command.Id);

        if (appointment.GoogleEventId != null && _calendar.IsConfigured)
            await _calendar.CancelAsync(appointment.GoogleEventId, cancellationToken);

        appointment.Cancel(command.Reason);
        await _appointments.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Отмена по просьбе самого человека из бота. Отдельно от <see cref="CancelAppointmentCommand"/>,
/// потому что здесь некому доверять: ключ бота общий, поэтому проверяем, что отменяют
/// именно свою запись.
/// </summary>
public record CancelOwnAppointmentCommand(Guid Id, long TelegramId, string? Reason = null) : ICommand;

public class CancelOwnAppointmentCommandHandler : ICommandHandler<CancelOwnAppointmentCommand>
{
    private readonly IAppointmentRepository _appointments;
    private readonly ISchedulingCalendar _calendar;

    public CancelOwnAppointmentCommandHandler(
        IAppointmentRepository appointments, ISchedulingCalendar calendar)
    {
        _appointments = appointments;
        _calendar = calendar;
    }

    public async Task Handle(CancelOwnAppointmentCommand command, CancellationToken cancellationToken)
    {
        var appointment = await _appointments.GetByIdAsync(command.Id, cancellationToken);

        // Чужую запись не отменяем и не подтверждаем, что она есть
        if (appointment == null || appointment.TelegramId != command.TelegramId)
            throw new AppointmentNotFoundException(command.Id);

        if (!appointment.IsActive)
            return;

        if (appointment.GoogleEventId != null && _calendar.IsConfigured)
            await _calendar.CancelAsync(appointment.GoogleEventId, cancellationToken);

        appointment.Cancel(command.Reason);
        await _appointments.SaveChangesAsync(cancellationToken);
    }
}

public record RescheduleAppointmentCommand(Guid Id, DateTime StartUtc) : ICommand<AppointmentDto>;

public class RescheduleAppointmentCommandHandler
    : ICommandHandler<RescheduleAppointmentCommand, AppointmentDto>
{
    private readonly IAvailabilitySettingsRepository _settings;
    private readonly IAppointmentRepository _appointments;
    private readonly ISchedulingCalendar _calendar;
    private readonly BusyTimeReader _busy;
    private readonly TimeProvider _clock;

    public RescheduleAppointmentCommandHandler(
        IAvailabilitySettingsRepository settings,
        IAppointmentRepository appointments,
        ISchedulingCalendar calendar,
        BusyTimeReader busy,
        TimeProvider clock)
    {
        _settings = settings;
        _appointments = appointments;
        _calendar = calendar;
        _busy = busy;
        _clock = clock;
    }

    public async Task<AppointmentDto> Handle(
        RescheduleAppointmentCommand command, CancellationToken cancellationToken)
    {
        var appointment = await _appointments.GetByIdAsync(command.Id, cancellationToken)
                          ?? throw new AppointmentNotFoundException(command.Id);

        var settings = await _settings.GetAsync(cancellationToken);
        if (!settings.Enabled)
            throw new BookingDisabledException();

        var startUtc = SchedulingTime.AsUtc(command.StartUtc);
        var busy = await _busy.ReadAsync(
            startUtc.AddDays(-1), startUtc.AddDays(1), cancellationToken, ignoreAppointmentId: appointment.Id);

        // The call being moved must not block its own new time, in our records or in Google
        var others = busy
            .Where(b => b.StartUtc != appointment.StartsAtUtc || b.EndUtc != appointment.EndsAtUtc)
            .ToList();

        if (!SlotPlanner.IsBookable(settings, others, _clock.GetUtcNow(), startUtc))
            throw new SlotUnavailableException(startUtc);

        if (appointment.GoogleEventId != null && _calendar.IsConfigured)
        {
            var moved = await _calendar.MoveAsync(
                appointment.GoogleEventId, startUtc, settings.SlotMinutes, cancellationToken);
            appointment.AttachCalendarEvent(moved.EventId, moved.MeetUrl, moved.HtmlLink);
        }

        appointment.MoveTo(startUtc, settings.SlotMinutes);
        await _appointments.SaveChangesAsync(cancellationToken);

        return appointment.ToDto();
    }
}

/// <summary>Closes a past call: it either happened or the person did not turn up.</summary>
public record CloseAppointmentCommand(Guid Id, bool NoShow) : ICommand;

public class CloseAppointmentCommandHandler : ICommandHandler<CloseAppointmentCommand>
{
    private readonly IAppointmentRepository _appointments;
    private readonly ILeadRepository _leads;

    public CloseAppointmentCommandHandler(IAppointmentRepository appointments, ILeadRepository leads)
    {
        _appointments = appointments;
        _leads = leads;
    }

    public async Task Handle(CloseAppointmentCommand command, CancellationToken cancellationToken)
    {
        var appointment = await _appointments.GetByIdAsync(command.Id, cancellationToken)
                          ?? throw new AppointmentNotFoundException(command.Id);

        if (command.NoShow)
            appointment.MarkNoShow();
        else
            appointment.MarkCompleted();

        await _appointments.SaveChangesAsync(cancellationToken);

        if (command.NoShow || appointment.LeadId is not { } leadId)
            return;

        var lead = await _leads.GetByIdAsync(leadId, cancellationToken);
        if (lead is { Status: LeadStatus.ConsultationScheduled })
        {
            lead.ChangeStatus(LeadStatus.ConsultationDone);
            await _leads.SaveChangesAsync(cancellationToken);
        }
    }
}

public record UpdateAvailabilitySettingsCommand(
    bool Enabled,
    string TimeZoneId,
    int SlotMinutes,
    int MinLeadTimeHours,
    int MaxDaysAhead,
    int BufferMinutes,
    IReadOnlyList<WorkingWindowDto> Windows) : ICommand<AvailabilitySettingsDto>;

public class UpdateAvailabilitySettingsCommandHandler
    : ICommandHandler<UpdateAvailabilitySettingsCommand, AvailabilitySettingsDto>
{
    private readonly IAvailabilitySettingsRepository _settings;

    public UpdateAvailabilitySettingsCommandHandler(IAvailabilitySettingsRepository settings)
    {
        _settings = settings;
    }

    public async Task<AvailabilitySettingsDto> Handle(
        UpdateAvailabilitySettingsCommand command, CancellationToken cancellationToken)
    {
        var settings = await _settings.GetAsync(cancellationToken);

        settings.Update(
            command.Enabled,
            command.TimeZoneId,
            command.SlotMinutes,
            command.MinLeadTimeHours,
            command.MaxDaysAhead,
            command.BufferMinutes,
            command.Windows.Select(SchedulingTime.ToWindow));

        await _settings.SaveAsync(settings, cancellationToken);
        return settings.ToDto();
    }
}
