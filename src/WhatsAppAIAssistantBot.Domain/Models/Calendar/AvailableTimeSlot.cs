namespace WhatsAppAIAssistantBot.Domain.Models.Calendar;

/// <summary>
/// Represents an available time slot for booking appointments
/// </summary>
public class AvailableTimeSlot
{
    /// <summary>
    /// The start time of the available slot
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// The end time of the available slot
    /// </summary>
    public DateTime EndTime { get; set; }
    
    /// <summary>
    /// The duration of the slot in minutes
    /// </summary>
    public int DurationMinutes { get; set; }
    
    /// <summary>
    /// A formatted display string for the time slot
    /// </summary>
    public string DisplayText { get; set; } = string.Empty;
}