using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WhatsAppAIAssistantBot.Domain.Models.Calendar;
using WhatsAppAIAssistantBot.Domain.Services.Calendar;
using WhatsAppAIAssistantBot.Infrastructure.Services.Calendar;

namespace WhatsAppAIAssistantBot.Tests;

/// <summary>
/// Unit tests for GoogleCalendarService
/// </summary>
public class GoogleCalendarServiceTests
{
    private readonly Mock<IGoogleCalendarApiWrapper> _mockCalendarApi;
    private readonly Mock<ILogger<GoogleCalendarService>> _mockLogger;
    private readonly GoogleCalendarConfiguration _configuration;
    private readonly IGoogleCalendarService _service;

    public GoogleCalendarServiceTests()
    {
        _mockCalendarApi = new Mock<IGoogleCalendarApiWrapper>();
        _mockLogger = new Mock<ILogger<GoogleCalendarService>>();
        
        _configuration = new GoogleCalendarConfiguration
        {
            CalendarId = "primary",
            ServiceAccountCredentialsPath = "test-credentials.json",
            TimeZone = "America/Argentina/Buenos_Aires",
            BusinessHours = new BusinessHours
            {
                StartHour = 10,
                EndHour = 18
            },
            DefaultSlotDurationMinutes = 30
        };

        var options = Options.Create(_configuration);
        _service = new TestableGoogleCalendarService(options, _mockLogger.Object, _mockCalendarApi.Object);
    }

    [Fact]
    public async Task GetAvailableTimeSlotsAsync_WithNoExistingEvents_ReturnsAvailableSlots()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 15, 0, 0, 0); // Monday
        var endDate = new DateTime(2024, 1, 15, 23, 59, 59);
        var emptyEvents = new List<CalendarEvent>();

        _mockCalendarApi.Setup(x => x.GetEventsAsync(
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyEvents);

        // Act
        var result = await _service.GetAvailableTimeSlotsAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count <= 3); // Should return max 3 slots
        Assert.True(result.Count > 0); // Should have at least one slot
        
        // Verify all slots are within business hours
        foreach (var slot in result)
        {
            Assert.True(slot.StartTime.Hour >= 10);
            Assert.True(slot.EndTime.Hour <= 18);
            Assert.Equal(30, slot.DurationMinutes);
        }
    }

    [Fact]
    public async Task GetAvailableTimeSlotsAsync_WithExistingEvents_ExcludesConflictingSlots()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 15, 0, 0, 0); // Monday
        var endDate = new DateTime(2024, 1, 15, 23, 59, 59);
        
        var existingEvents = new List<CalendarEvent>
        {
            new CalendarEvent
            {
                StartTime = new DateTime(2024, 1, 15, 10, 0, 0),
                EndTime = new DateTime(2024, 1, 15, 11, 0, 0)
            },
            new CalendarEvent
            {
                StartTime = new DateTime(2024, 1, 15, 14, 0, 0),
                EndTime = new DateTime(2024, 1, 15, 15, 0, 0)
            }
        };

        _mockCalendarApi.Setup(x => x.GetEventsAsync(
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvents);

        // Act
        var result = await _service.GetAvailableTimeSlotsAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        
        // Verify no slots conflict with existing events
        foreach (var slot in result)
        {
            Assert.False(slot.StartTime.Hour == 10 && slot.StartTime.Minute == 0);
            Assert.False(slot.StartTime.Hour == 10 && slot.StartTime.Minute == 30);
            Assert.False(slot.StartTime.Hour == 14 && slot.StartTime.Minute == 0);
            Assert.False(slot.StartTime.Hour == 14 && slot.StartTime.Minute == 30);
        }
    }

    [Fact]
    public async Task GetAvailableTimeSlotsAsync_WithWeekendDate_ReturnsEmptyList()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 13, 0, 0, 0); // Saturday
        var endDate = new DateTime(2024, 1, 13, 23, 59, 59);

        _mockCalendarApi.Setup(x => x.GetEventsAsync(
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CalendarEvent>());

        // Act
        var result = await _service.GetAvailableTimeSlotsAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task BookAppointmentAsync_WithAvailableSlot_ReturnsSuccessResult()
    {
        // Arrange
        var bookingRequest = new CalendarBookingRequest
        {
            StartTime = new DateTime(2024, 1, 15, 10, 0, 0),
            EndTime = new DateTime(2024, 1, 15, 10, 30, 0),
            Title = "Test Appointment",
            Description = "Test Description",
            AttendeeEmail = "test@example.com",
            AttendeeName = "John Doe",
            PhoneNumber = "+1234567890"
        };

        var createdEvent = new CalendarEvent
        {
            Id = "event123",
            HtmlLink = "https://calendar.google.com/event123"
        };

        // Mock availability check
        _mockCalendarApi.Setup(x => x.GetEventsAsync(
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CalendarEvent>());

        // Mock event creation
        _mockCalendarApi.Setup(x => x.CreateEventAsync(
            It.IsAny<string>(),
            It.IsAny<CalendarBookingRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdEvent);

        // Act
        var result = await _service.BookAppointmentAsync(bookingRequest);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("event123", result.EventId);
        Assert.Equal("https://calendar.google.com/event123", result.EventLink);
        Assert.Equal(bookingRequest, result.BookedAppointment);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task BookAppointmentAsync_WithUnavailableSlot_ReturnsFailureResult()
    {
        // Arrange
        var bookingRequest = new CalendarBookingRequest
        {
            StartTime = new DateTime(2024, 1, 15, 10, 0, 0),
            EndTime = new DateTime(2024, 1, 15, 10, 30, 0),
            Title = "Test Appointment",
            Description = "Test Description",
            AttendeeEmail = "test@example.com",
            AttendeeName = "John Doe",
            PhoneNumber = "+1234567890"
        };

        var conflictingEvent = new CalendarEvent
        {
            StartTime = new DateTime(2024, 1, 15, 10, 0, 0),
            EndTime = new DateTime(2024, 1, 15, 10, 30, 0)
        };

        // Mock availability check - return conflicting event
        _mockCalendarApi.Setup(x => x.GetEventsAsync(
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CalendarEvent> { conflictingEvent });

        // Act
        var result = await _service.BookAppointmentAsync(bookingRequest);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Equal("The selected time slot is no longer available.", result.ErrorMessage);
        Assert.Null(result.EventId);
        Assert.Null(result.EventLink);
    }

    [Fact]
    public async Task IsTimeSlotAvailableAsync_WithNoConflicts_ReturnsTrue()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 15, 10, 0, 0);
        var endTime = new DateTime(2024, 1, 15, 10, 30, 0);

        _mockCalendarApi.Setup(x => x.GetEventsAsync(
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CalendarEvent>());

        // Act
        var result = await _service.IsTimeSlotAvailableAsync(startTime, endTime);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsTimeSlotAvailableAsync_WithConflictingEvent_ReturnsFalse()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 15, 10, 0, 0);
        var endTime = new DateTime(2024, 1, 15, 10, 30, 0);

        var conflictingEvent = new CalendarEvent
        {
            StartTime = new DateTime(2024, 1, 15, 10, 15, 0),
            EndTime = new DateTime(2024, 1, 15, 10, 45, 0)
        };

        _mockCalendarApi.Setup(x => x.GetEventsAsync(
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CalendarEvent> { conflictingEvent });

        // Act
        var result = await _service.IsTimeSlotAvailableAsync(startTime, endTime);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetAvailableTimeSlotsAsync_WithApiException_ThrowsException()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 15, 0, 0, 0);
        var endDate = new DateTime(2024, 1, 15, 23, 59, 59);

        _mockCalendarApi.Setup(x => x.GetEventsAsync(
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => 
            _service.GetAvailableTimeSlotsAsync(startDate, endDate));
    }

    [Fact]
    public async Task BookAppointmentAsync_WithApiException_ReturnsFailureResult()
    {
        // Arrange
        var bookingRequest = new CalendarBookingRequest
        {
            StartTime = new DateTime(2024, 1, 15, 10, 0, 0),
            EndTime = new DateTime(2024, 1, 15, 10, 30, 0),
            Title = "Test Appointment",
            Description = "Test Description",
            AttendeeEmail = "test@example.com",
            AttendeeName = "John Doe",
            PhoneNumber = "+1234567890"
        };

        // Mock availability check - available
        _mockCalendarApi.Setup(x => x.GetEventsAsync(
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CalendarEvent>());

        // Mock event creation - throws exception
        _mockCalendarApi.Setup(x => x.CreateEventAsync(
            It.IsAny<string>(),
            It.IsAny<CalendarBookingRequest>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API Error"));

        // Act
        var result = await _service.BookAppointmentAsync(bookingRequest);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Contains("Failed to book appointment", result.ErrorMessage);
    }

    [Theory]
    [InlineData(2024, 1, 15, 9, 0, 0)] // Before business hours
    [InlineData(2024, 1, 15, 18, 30, 0)] // After business hours
    public async Task GetAvailableTimeSlotsAsync_OutsideBusinessHours_DoesNotIncludeSlots(
        int year, int month, int day, int hour, int minute, int second)
    {
        // Arrange
        var startDate = new DateTime(year, month, day, 0, 0, 0);
        var endDate = new DateTime(year, month, day, 23, 59, 59);

        _mockCalendarApi.Setup(x => x.GetEventsAsync(
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CalendarEvent>());

        // Act
        var result = await _service.GetAvailableTimeSlotsAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        
        // Verify no slots are outside business hours
        foreach (var slot in result)
        {
            Assert.True(slot.StartTime.Hour >= 10);
            Assert.True(slot.EndTime.Hour <= 18);
        }
    }

    [Fact]
    public async Task GetAvailableTimeSlotsAsync_WithCustomSlotDuration_ReturnsCorrectDuration()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 15, 0, 0, 0);
        var endDate = new DateTime(2024, 1, 15, 23, 59, 59);
        var customDuration = 60; // 1 hour

        _mockCalendarApi.Setup(x => x.GetEventsAsync(
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CalendarEvent>());

        // Act
        var result = await _service.GetAvailableTimeSlotsAsync(startDate, endDate, customDuration);

        // Assert
        Assert.NotNull(result);
        
        foreach (var slot in result)
        {
            Assert.Equal(customDuration, slot.DurationMinutes);
            Assert.Equal(customDuration, (slot.EndTime - slot.StartTime).TotalMinutes);
        }
    }
}

/// <summary>
/// Testable version of GoogleCalendarService that accepts a mock API wrapper
/// </summary>
public class TestableGoogleCalendarService : IGoogleCalendarService
{
    private readonly IGoogleCalendarApiWrapper _calendarApi;
    private readonly GoogleCalendarConfiguration _configuration;
    private readonly ILogger<GoogleCalendarService> _logger;

    public TestableGoogleCalendarService(
        IOptions<GoogleCalendarConfiguration> configuration,
        ILogger<GoogleCalendarService> logger,
        IGoogleCalendarApiWrapper calendarApi)
    {
        _configuration = configuration.Value;
        _logger = logger;
        _calendarApi = calendarApi;
    }

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
            
            var existingEvents = await _calendarApi.GetEventsAsync(
                _configuration.CalendarId, 
                startDate, 
                endDate.AddDays(1), 
                cancellationToken);
            
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var daySlots = GenerateDayTimeSlots(date, slotDurationMinutes, existingEvents, timeZone);
                availableSlots.AddRange(daySlots);
            }

            _logger.LogInformation("Found {Count} available time slots", availableSlots.Count);
            return availableSlots.Take(3).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available time slots");
            throw;
        }
    }

    public async Task<CalendarBookingResult> BookAppointmentAsync(
        CalendarBookingRequest bookingRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Booking appointment from {StartTime} to {EndTime}", 
                bookingRequest.StartTime, bookingRequest.EndTime);

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

            var createdEvent = await _calendarApi.CreateEventAsync(
                _configuration.CalendarId, 
                bookingRequest, 
                cancellationToken);

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

    public async Task<bool> IsTimeSlotAvailableAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var events = await _calendarApi.GetEventsAsync(
                _configuration.CalendarId,
                startTime.AddMinutes(-1),
                endTime.AddMinutes(1),
                cancellationToken);
            
            foreach (var existingEvent in events)
            {
                if (startTime < existingEvent.EndTime && endTime > existingEvent.StartTime)
                {
                    return false;
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

    private List<AvailableTimeSlot> GenerateDayTimeSlots(
        DateTime date,
        int slotDurationMinutes,
        IList<CalendarEvent> existingEvents,
        TimeZoneInfo timeZone)
    {
        var slots = new List<AvailableTimeSlot>();
        
        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
        {
            return slots;
        }

        var businessStart = new DateTime(date.Year, date.Month, date.Day, 
            _configuration.BusinessHours.StartHour, 0, 0);
        var businessEnd = new DateTime(date.Year, date.Month, date.Day, 
            _configuration.BusinessHours.EndHour, 0, 0);

        for (var slotStart = businessStart; 
             slotStart.AddMinutes(slotDurationMinutes) <= businessEnd; 
             slotStart = slotStart.AddMinutes(slotDurationMinutes))
        {
            var slotEnd = slotStart.AddMinutes(slotDurationMinutes);
            
            var hasConflict = existingEvents.Any(e => 
                slotStart < e.EndTime && slotEnd > e.StartTime);

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
}