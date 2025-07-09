namespace WhatsAppAIAssistantBot.Domain.Models.Calendar;

/// <summary>
/// Represents a request to book a calendar appointment
/// </summary>
public class CalendarBookingRequest
{
    /// <summary>
    /// The start time of the appointment
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// The end time of the appointment
    /// </summary>
    public DateTime EndTime { get; set; }
    
    /// <summary>
    /// The title/summary of the appointment
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional description of the appointment
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// The attendee's email address
    /// </summary>
    public string AttendeeEmail { get; set; } = string.Empty;
    
    /// <summary>
    /// The attendee's name
    /// </summary>
    public string AttendeeName { get; set; } = string.Empty;
    
    /// <summary>
    /// The phone number of the person booking (from WhatsApp)
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;
}