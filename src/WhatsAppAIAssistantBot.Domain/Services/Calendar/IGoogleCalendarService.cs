using WhatsAppAIAssistantBot.Domain.Models.Calendar;

namespace WhatsAppAIAssistantBot.Domain.Services.Calendar;

/// <summary>
/// Service for Google Calendar integration to handle appointment booking
/// </summary>
public interface IGoogleCalendarService
{
    /// <summary>
    /// Gets available time slots for booking appointments
    /// </summary>
    /// <param name="startDate">The start date to search for available slots</param>
    /// <param name="endDate">The end date to search for available slots</param>
    /// <param name="slotDurationMinutes">The duration of each slot in minutes (default: 30)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A list of available time slots</returns>
    Task<List<AvailableTimeSlot>> GetAvailableTimeSlotsAsync(
        DateTime startDate,
        DateTime endDate,
        int slotDurationMinutes = 30,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Books a calendar appointment
    /// </summary>
    /// <param name="bookingRequest">The booking request details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The booking result</returns>
    Task<CalendarBookingResult> BookAppointmentAsync(
        CalendarBookingRequest bookingRequest,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a specific time slot is available for booking
    /// </summary>
    /// <param name="startTime">The start time of the slot</param>
    /// <param name="endTime">The end time of the slot</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the slot is available, false otherwise</returns>
    Task<bool> IsTimeSlotAvailableAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);
}