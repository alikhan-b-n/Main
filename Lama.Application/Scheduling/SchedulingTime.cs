using System.Globalization;
using Lama.Domain.Scheduling.Entities;

namespace Lama.Application.Scheduling;

/// <summary>Small conversions shared by the scheduling commands and queries.</summary>
public static class SchedulingTime
{
    /// <summary>
    /// Times arrive from JSON with whatever kind the binder chose. Unspecified means the
    /// caller already sent UTC (the API documents it that way), so it is labelled, not shifted.
    /// </summary>
    public static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    /// <summary>The start of the call as the person sees it, e.g. "02.10.2025 15:00".</summary>
    public static string Local(Appointment appointment)
    {
        var zone = FindZone(appointment.TimeZoneId);
        return TimeZoneInfo.ConvertTimeFromUtc(appointment.StartsAtUtc, zone)
            .ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
    }

    public static WorkingWindow ToWindow(WorkingWindowDto dto) =>
        new(dto.Day, ParseTime(dto.Start, nameof(dto.Start)), ParseTime(dto.End, nameof(dto.End)));

    public static TimeOnly ParseTime(string value, string parameterName)
    {
        if (TimeOnly.TryParseExact(value?.Trim() ?? string.Empty, "HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed;

        throw new ArgumentException($"Time must look like 09:30, got '{value}'", parameterName);
    }

    /// <summary>What the consultant and the applicant see in the calendar invitation.</summary>
    public static CalendarEventRequest Describe(Appointment appointment)
    {
        var title = appointment.Kind == AppointmentKind.Consultation
            ? "IconicU: стратегическая консультация"
            : "IconicU: ознакомительный звонок";

        var lines = new List<string> { $"Заявка из Telegram-бота IconicU.", string.Empty, $"Имя: {appointment.FullName}", $"Email: {appointment.Email.Value}" };
        if (appointment.Phone != null)
            lines.Add($"Телефон: {appointment.Phone}");
        if (appointment.TelegramUsername != null)
            lines.Add($"Telegram: @{appointment.TelegramUsername}");
        if (appointment.Comment != null)
            lines.Add($"Комментарий: {appointment.Comment}");

        return new CalendarEventRequest(
            $"{title} — {appointment.FullName}",
            string.Join("\n", lines),
            appointment.StartsAtUtc,
            appointment.DurationMinutes,
            appointment.TimeZoneId,
            appointment.Email.Value,
            appointment.FullName);
    }

    private static TimeZoneInfo FindZone(string timeZoneId)
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
