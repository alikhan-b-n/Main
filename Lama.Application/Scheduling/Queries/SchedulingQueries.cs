using Lama.Application.Common;
using Lama.Application.LeadManagement;
using Lama.Domain.Scheduling;

namespace Lama.Application.Scheduling.Queries;

/// <summary>Open days with the number of free slots — the first screen of the bot's calendar.</summary>
public record GetAvailabilityQuery : IQuery<AvailabilityDto>;

public class GetAvailabilityQueryHandler : IQueryHandler<GetAvailabilityQuery, AvailabilityDto>
{
    private readonly IAvailabilitySettingsRepository _settings;
    private readonly BusyTimeReader _busy;
    private readonly TimeProvider _clock;

    public GetAvailabilityQueryHandler(
        IAvailabilitySettingsRepository settings, BusyTimeReader busy, TimeProvider clock)
    {
        _settings = settings;
        _busy = busy;
        _clock = clock;
    }

    public async Task<AvailabilityDto> Handle(GetAvailabilityQuery query, CancellationToken cancellationToken)
    {
        var settings = await _settings.GetAsync(cancellationToken);
        if (!settings.Enabled)
            return new AvailabilityDto(false, settings.TimeZoneId, settings.SlotMinutes, Array.Empty<AvailabilityDayDto>());

        var now = _clock.GetUtcNow();
        var busy = await _busy.ReadAsync(
            now.UtcDateTime, now.UtcDateTime.AddDays(settings.MaxDaysAhead + 1), cancellationToken);

        var days = SlotPlanner.Plan(settings, busy, now)
            .Select(day => new AvailabilityDayDto(day.Date, day.FreeCount, day.Slots.Count))
            .ToList();

        return new AvailabilityDto(true, settings.TimeZoneId, settings.SlotMinutes, days);
    }
}

/// <summary>Every slot of one day, free and taken alike.</summary>
public record GetDaySlotsQuery(DateOnly Date) : IQuery<DaySlotsDto>;

public class GetDaySlotsQueryHandler : IQueryHandler<GetDaySlotsQuery, DaySlotsDto>
{
    private readonly IAvailabilitySettingsRepository _settings;
    private readonly BusyTimeReader _busy;
    private readonly TimeProvider _clock;

    public GetDaySlotsQueryHandler(
        IAvailabilitySettingsRepository settings, BusyTimeReader busy, TimeProvider clock)
    {
        _settings = settings;
        _busy = busy;
        _clock = clock;
    }

    public async Task<DaySlotsDto> Handle(GetDaySlotsQuery query, CancellationToken cancellationToken)
    {
        var settings = await _settings.GetAsync(cancellationToken);
        var empty = new DaySlotsDto(query.Date, settings.TimeZoneId, settings.SlotMinutes, Array.Empty<SlotDto>());
        if (!settings.Enabled)
            return empty;

        var now = _clock.GetUtcNow();
        // A day in the consultant's zone can start the evening before in UTC, so ask for a wider window
        var from = query.Date.ToDateTime(TimeOnly.MinValue).AddDays(-1);
        var busy = await _busy.ReadAsync(
            DateTime.SpecifyKind(from, DateTimeKind.Utc),
            DateTime.SpecifyKind(from.AddDays(3), DateTimeKind.Utc),
            cancellationToken);

        var day = SlotPlanner.Plan(settings, busy, now, query.Date).FirstOrDefault();
        return day == null
            ? empty
            : new DaySlotsDto(day.Date, settings.TimeZoneId, settings.SlotMinutes,
                day.Slots.Select(s => s.ToDto()).ToList());
    }
}

public record GetAvailabilitySettingsQuery : IQuery<AvailabilitySettingsDto>;

public class GetAvailabilitySettingsQueryHandler
    : IQueryHandler<GetAvailabilitySettingsQuery, AvailabilitySettingsDto>
{
    private readonly IAvailabilitySettingsRepository _settings;

    public GetAvailabilitySettingsQueryHandler(IAvailabilitySettingsRepository settings)
    {
        _settings = settings;
    }

    public async Task<AvailabilitySettingsDto> Handle(
        GetAvailabilitySettingsQuery query, CancellationToken cancellationToken) =>
        (await _settings.GetAsync(cancellationToken)).ToDto();
}

public record GetAppointmentsQuery(AppointmentFilter Filter) : IQuery<PagedResult<AppointmentDto>>;

public class GetAppointmentsQueryHandler : IQueryHandler<GetAppointmentsQuery, PagedResult<AppointmentDto>>
{
    public const int MaxPageSize = 100;

    private readonly IAppointmentRepository _appointments;

    public GetAppointmentsQueryHandler(IAppointmentRepository appointments)
    {
        _appointments = appointments;
    }

    public async Task<PagedResult<AppointmentDto>> Handle(
        GetAppointmentsQuery query, CancellationToken cancellationToken)
    {
        var filter = query.Filter with
        {
            Page = Math.Max(1, query.Filter.Page),
            PageSize = Math.Clamp(query.Filter.PageSize, 1, MaxPageSize)
        };

        var page = await _appointments.SearchAsync(filter, cancellationToken);

        return new PagedResult<AppointmentDto>(
            page.Items.Select(a => a.ToDto()).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount);
    }
}

public record GetAppointmentByIdQuery(Guid Id) : IQuery<AppointmentDto?>;

public class GetAppointmentByIdQueryHandler : IQueryHandler<GetAppointmentByIdQuery, AppointmentDto?>
{
    private readonly IAppointmentRepository _appointments;

    public GetAppointmentByIdQueryHandler(IAppointmentRepository appointments)
    {
        _appointments = appointments;
    }

    public async Task<AppointmentDto?> Handle(GetAppointmentByIdQuery query, CancellationToken cancellationToken) =>
        (await _appointments.GetByIdAsync(query.Id, cancellationToken))?.ToDto();
}
