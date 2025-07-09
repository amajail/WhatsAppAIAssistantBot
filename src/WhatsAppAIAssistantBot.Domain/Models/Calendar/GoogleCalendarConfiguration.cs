namespace WhatsAppAIAssistantBot.Domain.Models.Calendar;

/// <summary>
/// Configuration options for Google Calendar integration
/// </summary>
public class GoogleCalendarConfiguration
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "GoogleCalendar";
    
    /// <summary>
    /// The Google Calendar ID to use (default: "primary")
    /// </summary>
    public string CalendarId { get; set; } = "primary";
    
    /// <summary>
    /// Path to the Google service account credentials JSON file
    /// </summary>
    public string ServiceAccountCredentialsPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Google service account credentials JSON content (alternative to file path)
    /// </summary>
    public string ServiceAccountCredentialsJson { get; set; } = string.Empty;
    
    /// <summary>
    /// Time zone for the calendar operations
    /// </summary>
    public string TimeZone { get; set; } = "America/Argentina/Buenos_Aires";
    
    /// <summary>
    /// Business hours configuration
    /// </summary>
    public BusinessHours BusinessHours { get; set; } = new();
    
    /// <summary>
    /// Default slot duration in minutes
    /// </summary>
    public int DefaultSlotDurationMinutes { get; set; } = 30;
}

/// <summary>
/// Business hours configuration
/// </summary>
public class BusinessHours
{
    /// <summary>
    /// Start hour of business hours (24-hour format)
    /// </summary>
    public int StartHour { get; set; } = 10;
    
    /// <summary>
    /// End hour of business hours (24-hour format)
    /// </summary>
    public int EndHour { get; set; } = 18;
}