using Microsoft.Extensions.Logging;
using Moq;
using WhatsAppAIAssistantBot.Application.Services;
using WhatsAppAIAssistantBot.Domain.Entities;
using WhatsAppAIAssistantBot.Domain.Models;
using WhatsAppAIAssistantBot.Domain.Services;
using Xunit;

namespace WhatsAppAIAssistantBot.Tests;

public class UserContextServiceTests
{
    private readonly Mock<ILocalizationService> _mockLocalizationService;
    private readonly Mock<ILogger<UserContextService>> _mockLogger;
    private readonly UserContextService _contextService;

    public UserContextServiceTests()
    {
        _mockLocalizationService = new Mock<ILocalizationService>();
        _mockLogger = new Mock<ILogger<UserContextService>>();

        _contextService = new UserContextService(
            _mockLocalizationService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task FormatUserContextAsync_WithMinimalLevel_ShouldReturnMinimalContext()
    {
        // Arrange
        var user = CreateTestUser();
        var message = "Hello there";
        var template = "[USER CONTEXT: Name: {0}]\n\nUser message: {1}";

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.ContextTemplateMinimal, user.LanguageCode))
            .ReturnsAsync(template);

        // Act
        var result = await _contextService.FormatUserContextAsync(user, message, ContextLevel.Minimal);

        // Assert
        var expected = "[USER CONTEXT: Name: John Doe]\n\nUser message: Hello there";
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task FormatUserContextAsync_WithStandardLevel_ShouldReturnStandardContext()
    {
        // Arrange
        var user = CreateTestUser();
        var message = "What's my email?";
        var template = "[USER CONTEXT: Name: {0}, Email: {1}, Language: {2}]\n\nUser message: {3}";

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.ContextTemplate, user.LanguageCode))
            .ReturnsAsync(template);

        // Act
        var result = await _contextService.FormatUserContextAsync(user, message, ContextLevel.Standard);

        // Assert
        var expected = "[USER CONTEXT: Name: John Doe, Email: john@example.com, Language: English]\n\nUser message: What's my email?";
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task FormatUserContextAsync_WithFullLevel_ShouldReturnFullContext()
    {
        // Arrange
        var user = CreateTestUser();
        var message = "Tell me about my account";
        var template = "[USER CONTEXT: Name: {0}, Email: {1}, Language: {2}, Member since: {3}, Timezone: {4}]\n\nUser message: {5}";

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.ContextTemplateFull, user.LanguageCode))
            .ReturnsAsync(template);

        // Act
        var result = await _contextService.FormatUserContextAsync(user, message, ContextLevel.Full);

        // Assert
        Assert.Contains("Name: John Doe", result);
        Assert.Contains("Email: john@example.com", result);
        Assert.Contains("Language: English", result);
        Assert.Contains("Member since: 2024-01-01", result);
        Assert.Contains("Tell me about my account", result);
    }

    [Fact]
    public async Task FormatUserContextAsync_WithNoneLevel_ShouldReturnOriginalMessage()
    {
        // Arrange
        var user = CreateTestUser();
        var message = "Just a regular message";

        // Act
        var result = await _contextService.FormatUserContextAsync(user, message, ContextLevel.None);

        // Assert
        Assert.Equal(message, result);
    }

    [Fact]
    public async Task ShouldIncludeContextAsync_WithSystemCommand_ShouldReturnFalse()
    {
        // Arrange
        var message = "/help";

        // Act
        var result = await _contextService.ShouldIncludeContextAsync(message);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ShouldIncludeContextAsync_WithPersonalQuestion_ShouldReturnTrue()
    {
        // Arrange
        var message = "What's my name?";
        var patterns = new[] { "what's my", "my name", "who am i" };

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.PersonalQuestionPatterns, "en"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(patterns));

        // Act
        var result = await _contextService.ShouldIncludeContextAsync(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ShouldIncludeContextAsync_WithRegularMessage_ShouldReturnTrue()
    {
        // Arrange
        var message = "How are you doing today?";
        var patterns = new[] { "what's my", "my name", "who am i" };

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.PersonalQuestionPatterns, "en"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(patterns));

        // Act
        var result = await _contextService.ShouldIncludeContextAsync(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DetermineContextLevelAsync_WithPersonalQuestion_ShouldReturnFull()
    {
        // Arrange
        var message = "What's my email address?";
        var namePatterns = new[] { "my name", "who am i" };
        var emailPatterns = new[] { "my email", "email address" };
        var personalPatterns = new[] { "what's my", "my info" };

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.NameQuestionPatterns, "en"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(namePatterns));

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.EmailQuestionPatterns, "en"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(emailPatterns));

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.PersonalQuestionPatterns, "en"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(personalPatterns));

        // Act
        var result = await _contextService.DetermineContextLevelAsync(message);

        // Assert
        Assert.Equal(ContextLevel.Full, result);
    }

    [Fact]
    public async Task DetermineContextLevelAsync_WithShortMessage_ShouldReturnMinimal()
    {
        // Arrange
        var message = "Hi";
        var namePatterns = new[] { "my name", "who am i" };
        var emailPatterns = new[] { "my email", "email address" };
        var personalPatterns = new[] { "what's my", "my info" };

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.NameQuestionPatterns, "en"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(namePatterns));

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.EmailQuestionPatterns, "en"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(emailPatterns));

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.PersonalQuestionPatterns, "en"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(personalPatterns));

        // Act
        var result = await _contextService.DetermineContextLevelAsync(message);

        // Assert
        Assert.Equal(ContextLevel.Minimal, result);
    }

    [Fact]
    public async Task DetermineContextLevelAsync_WithRegularMessage_ShouldReturnStandard()
    {
        // Arrange
        var message = "How can you help me today?";
        var namePatterns = new[] { "my name", "who am i" };
        var emailPatterns = new[] { "my email", "email address" };
        var personalPatterns = new[] { "what's my", "my info" };

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.NameQuestionPatterns, "en"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(namePatterns));

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.EmailQuestionPatterns, "en"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(emailPatterns));

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.PersonalQuestionPatterns, "en"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(personalPatterns));

        // Act
        var result = await _contextService.DetermineContextLevelAsync(message);

        // Assert
        Assert.Equal(ContextLevel.Standard, result);
    }

    private static User CreateTestUser()
    {
        return new User
        {
            PhoneNumber = "whatsapp:+1234567890",
            ThreadId = "thread_123",
            Name = "John Doe",
            Email = "john@example.com",
            LanguageCode = "en",
            CreatedAt = new DateTime(2024, 1, 1),
            UpdatedAt = DateTime.UtcNow
        };
    }
}