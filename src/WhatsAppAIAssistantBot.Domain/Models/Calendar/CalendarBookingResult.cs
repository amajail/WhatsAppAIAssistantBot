namespace WhatsAppAIAssistantBot.Domain.Models.Calendar;

/// <summary>
/// Represents the result of a calendar booking operation
/// </summary>
public class CalendarBookingResult
{
    /// <summary>
    /// Indicates whether the booking was successful
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// The Google Calendar event ID if booking was successful
    /// </summary>
    public string? EventId { get; set; }
    
    /// <summary>
    /// Error message if booking failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// The booked appointment details
    /// </summary>
    public CalendarBookingRequest? BookedAppointment { get; set; }
    
    /// <summary>
    /// The Google Calendar event link for the booked appointment
    /// </summary>
    public string? EventLink { get; set; }
}