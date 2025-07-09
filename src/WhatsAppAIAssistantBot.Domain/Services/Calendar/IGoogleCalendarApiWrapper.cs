using WhatsAppAIAssistantBot.Domain.Models.Calendar;

namespace WhatsAppAIAssistantBot.Domain.Services.Calendar;

/// <summary>
/// Calendar event model for domain layer
/// </summary>
public class CalendarEvent
{
    public string Id { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string HtmlLink { get; set; } = string.Empty;
}

/// <summary>
/// Wrapper interface for Google Calendar API to enable testability
/// </summary>
public interface IGoogleCalendarApiWrapper
{
    /// <summary>
    /// Gets events from the calendar within the specified time range
    /// </summary>
    /// <param name="calendarId">Calendar ID</param>
    /// <param name="timeMin">Minimum time</param>
    /// <param name="timeMax">Maximum time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of calendar events</returns>
    Task<IList<CalendarEvent>> GetEventsAsync(
        string calendarId, 
        DateTime timeMin, 
        DateTime timeMax, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new calendar event
    /// </summary>
    /// <param name="calendarId">Calendar ID</param>
    /// <param name="bookingRequest">Booking request details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created event</returns>
    Task<CalendarEvent> CreateEventAsync(
        string calendarId, 
        CalendarBookingRequest bookingRequest, 
        CancellationToken cancellationToken = default);
}