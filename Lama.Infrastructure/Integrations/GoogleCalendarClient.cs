using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Lama.Application.Scheduling;
using Lama.Domain.Scheduling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lama.Infrastructure.Integrations;

/// <summary>
/// Credentials for the consultant's Google account. Obtained once with
/// deploy/get-google-refresh-token.py and kept in user-secrets or the environment;
/// the refresh token is long-lived as long as the OAuth app stays published.
/// </summary>
public class GoogleCalendarOptions
{
    public const string SectionName = "Integrations:GoogleCalendar";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>"primary" is the main calendar of the account that granted access.</summary>
    public string CalendarId { get; set; } = "primary";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret)
        && !string.IsNullOrWhiteSpace(RefreshToken);
}

/// <summary>
/// Google Calendar behind <see cref="ISchedulingCalendar"/>. Every event is created with a
/// Meet conference, and the applicant is invited by email so the call lands in their calendar too.
/// </summary>
public class GoogleCalendarClient : ISchedulingCalendar, IDisposable
{
    private readonly GoogleCalendarOptions _options;
    private readonly ILogger<GoogleCalendarClient> _logger;
    private readonly Lazy<CalendarService> _service;

    public GoogleCalendarClient(IOptions<GoogleCalendarOptions> options, ILogger<GoogleCalendarClient> logger)
    {
        _options = options.Value;
        _logger = logger;
        _service = new Lazy<CalendarService>(CreateService);
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task<IReadOnlyList<BusyInterval>> GetBusyAsync(
        DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var request = _service.Value.Freebusy.Query(new FreeBusyRequest
        {
            TimeMinDateTimeOffset = new DateTimeOffset(fromUtc, TimeSpan.Zero),
            TimeMaxDateTimeOffset = new DateTimeOffset(toUtc, TimeSpan.Zero),
            Items = new List<FreeBusyRequestItem> { new() { Id = _options.CalendarId } }
        });

        var response = await ExecuteAsync(() => request.ExecuteAsync(cancellationToken), "read busy time");

        if (!response.Calendars.TryGetValue(_options.CalendarId, out var calendar))
            return Array.Empty<BusyInterval>();

        if (calendar.Errors is { Count: > 0 })
            throw new CalendarUnavailableException(
                "Google could not read the calendar: " + string.Join("; ", calendar.Errors.Select(e => e.Reason)));

        return (calendar.Busy ?? new List<TimePeriod>())
            .Where(period => period.StartDateTimeOffset.HasValue && period.EndDateTimeOffset.HasValue)
            .Select(period => new BusyInterval(
                period.StartDateTimeOffset!.Value.UtcDateTime,
                period.EndDateTimeOffset!.Value.UtcDateTime))
            .ToList();
    }

    public async Task<CalendarEvent> CreateAsync(
        CalendarEventRequest request, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var body = new Event
        {
            Summary = request.Summary,
            Description = request.Description,
            Start = At(request.StartUtc),
            End = At(request.StartUtc.AddMinutes(request.DurationMinutes)),
            // Google needs a request id of its own to make conference creation idempotent
            ConferenceData = new ConferenceData
            {
                CreateRequest = new CreateConferenceRequest
                {
                    RequestId = Guid.NewGuid().ToString("N"),
                    ConferenceSolutionKey = new ConferenceSolutionKey { Type = "hangoutsMeet" }
                }
            }
        };

        if (!string.IsNullOrWhiteSpace(request.AttendeeEmail))
            body.Attendees = new List<EventAttendee>
            {
                new() { Email = request.AttendeeEmail, DisplayName = request.AttendeeName }
            };

        var insert = _service.Value.Events.Insert(body, _options.CalendarId);
        insert.ConferenceDataVersion = 1;
        insert.SendUpdates = EventsResource.InsertRequest.SendUpdatesEnum.All;

        var created = await ExecuteAsync(() => insert.ExecuteAsync(cancellationToken), "create the event");
        return Describe(created);
    }

    public async Task<CalendarEvent> MoveAsync(
        string eventId, DateTime startUtc, int durationMinutes, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var patch = _service.Value.Events.Patch(new Event
        {
            Start = At(startUtc),
            End = At(startUtc.AddMinutes(durationMinutes))
        }, _options.CalendarId, eventId);
        patch.SendUpdates = EventsResource.PatchRequest.SendUpdatesEnum.All;

        var moved = await ExecuteAsync(() => patch.ExecuteAsync(cancellationToken), "move the event");
        return Describe(moved);
    }

    public async Task CancelAsync(string eventId, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var delete = _service.Value.Events.Delete(_options.CalendarId, eventId);
        delete.SendUpdates = EventsResource.DeleteRequest.SendUpdatesEnum.All;

        try
        {
            await delete.ExecuteAsync(cancellationToken);
        }
        // Someone deleted it in Google already: the outcome we wanted is the outcome we have
        catch (GoogleApiException ex) when (
            ex.HttpStatusCode is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.Gone)
        {
            _logger.LogInformation("Calendar event {EventId} was already gone", eventId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new CalendarUnavailableException($"Could not cancel the event: {ex.Message}", ex);
        }
    }

    /// <summary>Times are sent as UTC instants; the calendar shows them in its own zone.</summary>
    private static EventDateTime At(DateTime utc) => new()
    {
        DateTimeDateTimeOffset = new DateTimeOffset(utc, TimeSpan.Zero),
        TimeZone = "UTC"
    };

    private static CalendarEvent Describe(Event calendarEvent) => new(
        calendarEvent.Id,
        calendarEvent.ConferenceData?.EntryPoints?
            .FirstOrDefault(entry => entry.EntryPointType == "video")?.Uri
        ?? calendarEvent.HangoutLink,
        calendarEvent.HtmlLink);

    private async Task<T> ExecuteAsync<T>(Func<Task<T>> call, string what)
    {
        try
        {
            return await call();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Google Calendar failed to {What}", what);
            throw new CalendarUnavailableException($"Google Calendar could not {what}: {ex.Message}", ex);
        }
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new CalendarUnavailableException("Google Calendar is not configured");
    }

    private CalendarService CreateService()
    {
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = _options.ClientId, ClientSecret = _options.ClientSecret },
            Scopes = new[] { CalendarService.Scope.CalendarEvents, CalendarService.Scope.CalendarReadonly }
        });

        // The access token is refreshed by the library whenever it expires
        var credential = new UserCredential(flow, "iconicu", new TokenResponse { RefreshToken = _options.RefreshToken });

        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "IconicU CRM"
        });
    }

    public void Dispose()
    {
        if (_service.IsValueCreated)
            _service.Value.Dispose();
        GC.SuppressFinalize(this);
    }
}
