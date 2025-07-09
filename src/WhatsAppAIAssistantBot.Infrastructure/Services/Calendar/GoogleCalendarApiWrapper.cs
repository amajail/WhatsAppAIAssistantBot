using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using WhatsAppAIAssistantBot.Domain.Services.Calendar;
using WhatsAppAIAssistantBot.Domain.Models.Calendar;

namespace WhatsAppAIAssistantBot.Infrastructure.Services.Calendar;

/// <summary>
/// Wrapper for Google Calendar API to enable testability
/// </summary>
public class GoogleCalendarApiWrapper : IGoogleCalendarApiWrapper
{
    private readonly CalendarService _calendarService;
    private readonly GoogleCalendarConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the GoogleCalendarApiWrapper
    /// </summary>
    /// <param name="calendarService">Calendar service instance</param>
    /// <param name="configuration">Configuration</param>
    public GoogleCalendarApiWrapper(CalendarService calendarService, GoogleCalendarConfiguration configuration)
    {
        _calendarService = calendarService;
        _configuration = configuration;
    }

    /// <summary>
    /// Gets events from the calendar within the specified time range
    /// </summary>
    /// <param name="calendarId">Calendar ID</param>
    /// <param name="timeMin">Minimum time</param>
    /// <param name="timeMax">Maximum time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of calendar events</returns>
    public async Task<IList<CalendarEvent>> GetEventsAsync(
        string calendarId, 
        DateTime timeMin, 
        DateTime timeMax, 
        CancellationToken cancellationToken = default)
    {
        var request = _calendarService.Events.List(calendarId);
        request.TimeMin = timeMin;
        request.TimeMax = timeMax;
        request.SingleEvents = true;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

        var events = await request.ExecuteAsync(cancellationToken);
        
        return events.Items.Select(e => new CalendarEvent
        {
            Id = e.Id,
            StartTime = e.Start?.DateTime ?? DateTime.MinValue,
            EndTime = e.End?.DateTime ?? DateTime.MinValue,
            Summary = e.Summary ?? string.Empty,
            Description = e.Description ?? string.Empty,
            HtmlLink = e.HtmlLink ?? string.Empty
        }).ToList();
    }

    /// <summary>
    /// Creates a new calendar event
    /// </summary>
    /// <param name="calendarId">Calendar ID</param>
    /// <param name="bookingRequest">Booking request details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created event</returns>
    public async Task<CalendarEvent> CreateEventAsync(
        string calendarId, 
        CalendarBookingRequest bookingRequest, 
        CancellationToken cancellationToken = default)
    {
        var calendarEvent = new Event
        {
            Summary = bookingRequest.Title,
            Description = $"{bookingRequest.Description}\n\nBooked via WhatsApp by: {bookingRequest.AttendeeName} ({bookingRequest.PhoneNumber})",
            Start = new EventDateTime
            {
                DateTime = bookingRequest.StartTime,
                TimeZone = _configuration.TimeZone
            },
            End = new EventDateTime
            {
                DateTime = bookingRequest.EndTime,
                TimeZone = _configuration.TimeZone
            },
            Attendees = new List<EventAttendee>
            {
                new EventAttendee
                {
                    Email = bookingRequest.AttendeeEmail,
                    DisplayName = bookingRequest.AttendeeName
                }
            }
        };

        var request = _calendarService.Events.Insert(calendarEvent, calendarId);
        var createdEvent = await request.ExecuteAsync(cancellationToken);
        
        return new CalendarEvent
        {
            Id = createdEvent.Id,
            StartTime = createdEvent.Start?.DateTime ?? DateTime.MinValue,
            EndTime = createdEvent.End?.DateTime ?? DateTime.MinValue,
            Summary = createdEvent.Summary ?? string.Empty,
            Description = createdEvent.Description ?? string.Empty,
            HtmlLink = createdEvent.HtmlLink ?? string.Empty
        };
    }
}