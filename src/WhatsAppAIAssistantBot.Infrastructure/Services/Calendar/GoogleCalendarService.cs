using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsAppAIAssistantBot.Domain.Models.Calendar;
using WhatsAppAIAssistantBot.Domain.Services.Calendar;

namespace WhatsAppAIAssistantBot.Infrastructure.Services.Calendar;

/// <summary>
/// Google Calendar service implementation for appointment booking
/// </summary>
public class GoogleCalendarService : IGoogleCalendarService
{
    private readonly CalendarService _calendarService;
    private readonly GoogleCalendarConfiguration _configuration;
    private readonly ILogger<GoogleCalendarService> _logger;

    /// <summary>
    /// Initializes a new instance of the GoogleCalendarService
    /// </summary>
    /// <param name="configuration">Google Calendar configuration</param>
    /// <param name="logger">Logger instance</param>
    public GoogleCalendarService(
        IOptions<GoogleCalendarConfiguration> configuration,
        ILogger<GoogleCalendarService> logger)
    {
        _configuration = configuration.Value;
        _logger = logger;
        _calendarService = InitializeCalendarService();
    }

    /// <summary>
    /// Gets available time slots for booking appointments
    /// </summary>
    /// <param name="startDate">The start date to search for available slots</param>
    /// <param name="endDate">The end date to search for available slots</param>
    /// <param name="slotDurationMinutes">The duration of each slot in minutes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A list of available time slots</returns>
    public async Task<List<AvailableTimeSlot>> GetAvailableTimeSlotsAsync(
        DateTime startDate,
        DateTime endDate,
        int slotDurationMinutes = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting available time slots from {StartDate} to {EndDate}", 
                startDate, endDate);

            var availableSlots = new List<AvailableTimeSlot>();
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_configuration.TimeZone);
            
            // Get existing events in the date range
            var existingEvents = await GetExistingEventsAsync(startDate, endDate, cancellationToken);
            
            // Generate time slots for each day
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var daySlots = GenerateDayTimeSlots(date, slotDurationMinutes, existingEvents, timeZone);
                availableSlots.AddRange(daySlots);
            }

            _logger.LogInformation("Found {Count} available time slots", availableSlots.Count);
            return availableSlots.Take(3).ToList(); // Return only 3 slots as requested
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available time slots");
            throw;
        }
    }

    /// <summary>
    /// Books a calendar appointment
    /// </summary>
    /// <param name="bookingRequest">The booking request details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The booking result</returns>
    public async Task<CalendarBookingResult> BookAppointmentAsync(
        CalendarBookingRequest bookingRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Booking appointment from {StartTime} to {EndTime}", 
                bookingRequest.StartTime, bookingRequest.EndTime);

            // Check if slot is still available
            var isAvailable = await IsTimeSlotAvailableAsync(
                bookingRequest.StartTime, 
                bookingRequest.EndTime, 
                cancellationToken);

            if (!isAvailable)
            {
                return new CalendarBookingResult
                {
                    IsSuccess = false,
                    ErrorMessage = "The selected time slot is no longer available."
                };
            }

            // Create the calendar event
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

            var request = _calendarService.Events.Insert(calendarEvent, _configuration.CalendarId);
            var createdEvent = await request.ExecuteAsync(cancellationToken);

            _logger.LogInformation("Successfully booked appointment with event ID: {EventId}", 
                createdEvent.Id);

            return new CalendarBookingResult
            {
                IsSuccess = true,
                EventId = createdEvent.Id,
                BookedAppointment = bookingRequest,
                EventLink = createdEvent.HtmlLink
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error booking appointment");
            return new CalendarBookingResult
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to book appointment: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Checks if a specific time slot is available for booking
    /// </summary>
    /// <param name="startTime">The start time of the slot</param>
    /// <param name="endTime">The end time of the slot</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the slot is available, false otherwise</returns>
    public async Task<bool> IsTimeSlotAvailableAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = _calendarService.Events.List(_configuration.CalendarId);
            request.TimeMin = startTime.AddMinutes(-1);
            request.TimeMax = endTime.AddMinutes(1);
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            var events = await request.ExecuteAsync(cancellationToken);
            
            // Check if any existing event overlaps with the requested time slot
            foreach (var existingEvent in events.Items)
            {
                if (existingEvent.Start?.DateTime != null && existingEvent.End?.DateTime != null)
                {
                    var existingStart = existingEvent.Start.DateTime.Value;
                    var existingEnd = existingEvent.End.DateTime.Value;
                    
                    // Check for overlap
                    if (startTime < existingEnd && endTime > existingStart)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking time slot availability");
            return false;
        }
    }

    /// <summary>
    /// Initializes the Google Calendar service
    /// </summary>
    /// <returns>Configured CalendarService instance</returns>
    private CalendarService InitializeCalendarService()
    {
        try
        {
            GoogleCredential credential;
            
            // Try to load from JSON content first (environment variable)
            if (!string.IsNullOrEmpty(_configuration.ServiceAccountCredentialsJson))
            {
                _logger.LogInformation("Loading Google Calendar credentials from JSON content");
                credential = GoogleCredential.FromJson(_configuration.ServiceAccountCredentialsJson)
                    .CreateScoped(CalendarService.Scope.Calendar);
            }
            // Fall back to file path if JSON content not available
            else if (!string.IsNullOrEmpty(_configuration.ServiceAccountCredentialsPath))
            {
                _logger.LogInformation("Loading Google Calendar credentials from file path");
                credential = GoogleCredential.FromFile(_configuration.ServiceAccountCredentialsPath)
                    .CreateScoped(CalendarService.Scope.Calendar);
            }
            else
            {
                throw new InvalidOperationException("Google Calendar credentials not configured. Please set either ServiceAccountCredentialsJson or ServiceAccountCredentialsPath.");
            }

            return new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "WhatsApp AI Assistant Bot"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Google Calendar service");
            throw;
        }
    }

    /// <summary>
    /// Gets existing events within the specified date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of existing events</returns>
    private async Task<List<Event>> GetExistingEventsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var request = _calendarService.Events.List(_configuration.CalendarId);
        request.TimeMin = startDate;
        request.TimeMax = endDate.AddDays(1);
        request.SingleEvents = true;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

        var events = await request.ExecuteAsync(cancellationToken);
        return events.Items.ToList();
    }

    /// <summary>
    /// Generates available time slots for a specific day
    /// </summary>
    /// <param name="date">The date to generate slots for</param>
    /// <param name="slotDurationMinutes">Duration of each slot in minutes</param>
    /// <param name="existingEvents">List of existing events</param>
    /// <param name="timeZone">Time zone information</param>
    /// <returns>List of available time slots for the day</returns>
    private List<AvailableTimeSlot> GenerateDayTimeSlots(
        DateTime date,
        int slotDurationMinutes,
        List<Event> existingEvents,
        TimeZoneInfo timeZone)
    {
        var slots = new List<AvailableTimeSlot>();
        
        // Skip weekends (optional - you can remove this if you want weekend bookings)
        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
        {
            return slots;
        }

        var businessStart = new DateTime(date.Year, date.Month, date.Day, 
            _configuration.BusinessHours.StartHour, 0, 0);
        var businessEnd = new DateTime(date.Year, date.Month, date.Day, 
            _configuration.BusinessHours.EndHour, 0, 0);

        // Generate all possible slots within business hours
        for (var slotStart = businessStart; 
             slotStart.AddMinutes(slotDurationMinutes) <= businessEnd; 
             slotStart = slotStart.AddMinutes(slotDurationMinutes))
        {
            var slotEnd = slotStart.AddMinutes(slotDurationMinutes);
            
            // Check if this slot conflicts with any existing event
            var hasConflict = existingEvents.Any(e => 
                e.Start?.DateTime != null && e.End?.DateTime != null &&
                slotStart < e.End.DateTime && slotEnd > e.Start.DateTime);

            if (!hasConflict)
            {
                slots.Add(new AvailableTimeSlot
                {
                    StartTime = slotStart,
                    EndTime = slotEnd,
                    DurationMinutes = slotDurationMinutes,
                    DisplayText = $"{slotStart:dddd, MMMM dd} at {slotStart:HH:mm} - {slotEnd:HH:mm}"
                });
            }
        }

        return slots;
    }

    /// <summary>
    /// Disposes the calendar service
    /// </summary>
    public void Dispose()
    {
        _calendarService?.Dispose();
    }
}