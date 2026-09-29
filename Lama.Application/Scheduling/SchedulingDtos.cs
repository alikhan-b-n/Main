using Lama.Domain.Scheduling;
using Lama.Domain.Scheduling.Entities;

namespace Lama.Application.Scheduling;

/// <summary>Days the bot offers first, before anyone picks a time.</summary>
public record AvailabilityDto(
    bool Enabled,
    string TimeZoneId,
    int SlotMinutes,
    IReadOnlyList<AvailabilityDayDto> Days);

public record AvailabilityDayDto(DateOnly Date, int FreeCount, int TotalCount);

public record DaySlotsDto(
    DateOnly Date,
    string TimeZoneId,
    int SlotMinutes,
    IReadOnlyList<SlotDto> Slots);

/// <summary>
/// One time on the grid. <paramref name="StartUtc"/> is what a booking refers to;
/// <paramref name="LocalTime"/> is the same moment as the person sees it, so clients
/// do not need a time zone database of their own.
/// </summary>
public record SlotDto(DateTime StartUtc, string LocalTime, bool IsFree);

public record AppointmentDto(
    Guid Id,
    Guid? LeadId,
    string FullName,
    string Email,
    string? Phone,
    string? TelegramUsername,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    int DurationMinutes,
    string TimeZoneId,
    string LocalTime,
    string Kind,
    string Status,
    string? MeetUrl,
    string? CalendarLink,
    string? Comment,
    string? CancelReason,
    DateTime CreatedAt);

public record AvailabilitySettingsDto(
    bool Enabled,
    string TimeZoneId,
    int SlotMinutes,
    int MinLeadTimeHours,
    int MaxDaysAhead,
    int BufferMinutes,
    IReadOnlyList<WorkingWindowDto> Windows);

/// <summary>Times as "HH:mm" — the format both the CRM form and JSON use.</summary>
public record WorkingWindowDto(DayOfWeek Day, string Start, string End);

public static class SchedulingMapping
{
    public const string TimeFormat = @"HH\:mm";

    public static AvailabilitySettingsDto ToDto(this AvailabilitySettings settings) => new(
        settings.Enabled,
        settings.TimeZoneId,
        settings.SlotMinutes,
        settings.MinLeadTimeHours,
        settings.MaxDaysAhead,
        settings.BufferMinutes,
        settings.Windows.Select(w => new WorkingWindowDto(
            w.Day,
            w.Start.ToString(TimeFormat),
            w.End.ToString(TimeFormat))).ToList());

    public static SlotDto ToDto(this CallSlot slot) =>
        new(slot.StartUtc, TimeOnly.FromDateTime(slot.LocalStart).ToString(TimeFormat), slot.IsFree);

    public static AppointmentDto ToDto(this Appointment appointment)
    {
        var zone = ResolveZone(appointment.TimeZoneId);
        var local = TimeZoneInfo.ConvertTimeFromUtc(appointment.StartsAtUtc, zone);

        return new AppointmentDto(
            appointment.Id,
            appointment.LeadId,
            appointment.FullName,
            appointment.Email.Value,
            appointment.Phone,
            appointment.TelegramUsername,
            DateTime.SpecifyKind(appointment.StartsAtUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(appointment.EndsAtUtc, DateTimeKind.Utc),
            appointment.DurationMinutes,
            appointment.TimeZoneId,
            local.ToString("yyyy-MM-dd HH:mm"),
            appointment.Kind.ToString(),
            appointment.Status.ToString(),
            appointment.MeetUrl,
            appointment.CalendarLink,
            appointment.Comment,
            appointment.CancelReason,
            DateTime.SpecifyKind(appointment.CreatedAt, DateTimeKind.Utc));
    }

    /// <summary>A stored zone that the server no longer knows must not break the list.</summary>
    private static TimeZoneInfo ResolveZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
