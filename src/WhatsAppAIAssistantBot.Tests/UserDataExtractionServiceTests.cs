using Microsoft.Extensions.Logging;
using Moq;
using WhatsAppAIAssistantBot.Domain.Services;
using WhatsAppAIAssistantBot.Application.Services;
using WhatsAppAIAssistantBot.Application;
using Xunit;

namespace WhatsAppAIAssistantBot.Tests;

public class UserDataExtractionServiceTests
{
    private readonly Mock<ILocalizationService> _mockLocalizationService;
    private readonly Mock<IAssistantService> _mockAssistantService;
    private readonly Mock<ILogger<UserDataExtractionService>> _mockLogger;
    private readonly UserDataExtractionService _extractionService;

    public UserDataExtractionServiceTests()
    {
        _mockLocalizationService = new Mock<ILocalizationService>();
        _mockAssistantService = new Mock<IAssistantService>();
        _mockLogger = new Mock<ILogger<UserDataExtractionService>>();
        
        _extractionService = new UserDataExtractionService(
            _mockLocalizationService.Object,
            _mockAssistantService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExtractNameAsync_WithValidNamePattern_ShouldReturnPatternMatchingResult()
    {
        // Arrange
        var message = "Name: John Doe";
        var languageCode = "en";
        var patterns = new[] { "name:", "my name is", "i am", "call me" };

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.NamePatterns, languageCode))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(patterns));

        // Act
        var result = await _extractionService.ExtractNameAsync(message, languageCode);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal("John Doe", result.ExtractedValue);
        Assert.Equal(ExtractionMethod.PatternMatching, result.Method);
        Assert.Equal(0.9, result.Confidence);
    }

    [Fact]
    public async Task ExtractNameAsync_WithSpanishPattern_ShouldReturnPatternMatchingResult()
    {
        // Arrange
        var message = "Nombre: Juan Pérez";
        var languageCode = "es";
        var patterns = new[] { "nombre:", "mi nombre es", "soy", "me llamo" };

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.NamePatterns, languageCode))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(patterns));

        // Act
        var result = await _extractionService.ExtractNameAsync(message, languageCode);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal("Juan Pérez", result.ExtractedValue);
        Assert.Equal(ExtractionMethod.PatternMatching, result.Method);
        Assert.Equal(0.9, result.Confidence);
    }

    [Fact]
    public async Task ExtractEmailAsync_WithValidEmailPattern_ShouldReturnPatternMatchingResult()
    {
        // Arrange
        var message = "Email: john@example.com";
        var languageCode = "en";
        var patterns = new[] { "email:", "my email is", "contact me at" };

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.EmailPatterns, languageCode))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(patterns));

        // Act
        var result = await _extractionService.ExtractEmailAsync(message, languageCode);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal("john@example.com", result.ExtractedValue);
        Assert.Equal(ExtractionMethod.PatternMatching, result.Method);
        Assert.Equal(0.9, result.Confidence);
    }

    [Fact]
    public async Task ExtractEmailAsync_WithEmailRegex_ShouldReturnPatternMatchingResult()
    {
        // Arrange
        var message = "You can reach me at test@domain.com for more info";
        var languageCode = "en";
        var patterns = new[] { "email:", "my email is", "contact me at" };

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.EmailPatterns, languageCode))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(patterns));

        // Act
        var result = await _extractionService.ExtractEmailAsync(message, languageCode);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal("test@domain.com", result.ExtractedValue);
        Assert.Equal(ExtractionMethod.PatternMatching, result.Method);
        Assert.Equal(0.8, result.Confidence);
    }

    [Fact]
    public async Task ExtractNameAsync_WithLLMFallback_ShouldReturnLLMResult()
    {
        // Arrange
        var message = "Hi there, I'm Sarah and I'd like to register";
        var languageCode = "en";
        var patterns = new[] { "name:", "my name is", "i am", "call me" };
        var threadId = "temp_thread_123";
        var llmResponse = "Sarah";

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.NamePatterns, languageCode))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(patterns));

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.LlmExtractNamePrompt, languageCode, message))
            .ReturnsAsync($"Extract only the person's name from the following message. If no name is found, respond 'NO_NAME_FOUND'. Message: {message}");

        _mockAssistantService.Setup(x => x.CreateOrGetThreadAsync(It.IsAny<string>()))
            .ReturnsAsync(threadId);

        _mockAssistantService.Setup(x => x.GetAssistantReplyAsync(threadId, It.IsAny<string>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _extractionService.ExtractNameAsync(message, languageCode);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal("Sarah", result.ExtractedValue);
        Assert.Equal(ExtractionMethod.LLMFallback, result.Method);
        Assert.Equal(0.7, result.Confidence);
    }

    [Fact]
    public async Task ExtractEmailAsync_WithLLMFallback_ShouldReturnLLMResult()
    {
        // Arrange - using a message that doesn't match patterns but contains an email that would need LLM to extract
        var message = "You can reach out via sarah dot johnson at company dot org";
        var languageCode = "en";
        var patterns = new[] { "email:", "my email is", "contact me at" };
        var threadId = "temp_thread_123";
        var llmResponse = "sarah.johnson@company.org";

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.EmailPatterns, languageCode))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(patterns));

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.LlmExtractEmailPrompt, languageCode, message))
            .ReturnsAsync($"Extract only the email address from the following message. If no email is found, respond 'NO_EMAIL_FOUND'. Message: {message}");

        _mockAssistantService.Setup(x => x.CreateOrGetThreadAsync(It.IsAny<string>()))
            .ReturnsAsync(threadId);

        _mockAssistantService.Setup(x => x.GetAssistantReplyAsync(threadId, It.IsAny<string>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _extractionService.ExtractEmailAsync(message, languageCode);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal("sarah.johnson@company.org", result.ExtractedValue);
        Assert.Equal(ExtractionMethod.LLMFallback, result.Method);
        Assert.Equal(0.7, result.Confidence);
    }

    [Fact]
    public async Task ExtractUserDataAsync_WithBothNameAndEmail_ShouldReturnBothResults()
    {
        // Arrange
        var message = "Hi, I'm John Smith and you can email me at john.smith@email.com";
        var languageCode = "en";
        var threadId = "temp_thread_123";
        var llmResponse = "{\"name\": \"John Smith\", \"email\": \"john.smith@email.com\"}";

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.LlmExtractBothPrompt, languageCode, message))
            .ReturnsAsync($"Extract the name and email from the following message. Respond in JSON format: {{\"name\": \"name or null\", \"email\": \"email or null\"}}. Message: {message}");

        _mockAssistantService.Setup(x => x.CreateOrGetThreadAsync(It.IsAny<string>()))
            .ReturnsAsync(threadId);

        _mockAssistantService.Setup(x => x.GetAssistantReplyAsync(threadId, It.IsAny<string>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _extractionService.ExtractUserDataAsync(message, languageCode);

        // Assert
        Assert.True(result.HasAnyData);
        Assert.True(result.Name?.IsSuccessful);
        Assert.Equal("John Smith", result.Name?.ExtractedValue);
        Assert.True(result.Email?.IsSuccessful);
        Assert.Equal("john.smith@email.com", result.Email?.ExtractedValue);
        Assert.Equal(ExtractionMethod.LLMFallback, result.Name?.Method);
        Assert.Equal(ExtractionMethod.LLMFallback, result.Email?.Method);
    }

    [Fact]
    public async Task ExtractNameAsync_WithInvalidName_ShouldReturnFailedResult()
    {
        // Arrange
        var message = "Name: X"; // Too short
        var languageCode = "en";
        var patterns = new[] { "name:", "my name is", "i am", "call me" };

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.NamePatterns, languageCode))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(patterns));

        // Act
        var result = await _extractionService.ExtractNameAsync(message, languageCode);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal(ExtractionMethod.Failed, result.Method);
    }

    [Fact]
    public async Task ExtractEmailAsync_WithInvalidEmail_ShouldReturnFailedResult()
    {
        // Arrange
        var message = "Email: not-an-email";
        var languageCode = "en";
        var patterns = new[] { "email:", "my email is", "contact me at" };

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.EmailPatterns, languageCode))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(patterns));

        // Act
        var result = await _extractionService.ExtractEmailAsync(message, languageCode);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal(ExtractionMethod.Failed, result.Method);
    }

    [Fact]
    public async Task ExtractNameAsync_WithLLMNotFound_ShouldReturnFailedResult()
    {
        // Arrange
        var message = "Just some random text without a name";
        var languageCode = "en";
        var patterns = new[] { "name:", "my name is", "i am", "call me" };
        var threadId = "temp_thread_123";
        var llmResponse = "NO_NAME_FOUND";

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.NamePatterns, languageCode))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(patterns));

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.LlmExtractNamePrompt, languageCode, message))
            .ReturnsAsync($"Extract only the person's name from the following message. If no name is found, respond 'NO_NAME_FOUND'. Message: {message}");

        _mockAssistantService.Setup(x => x.CreateOrGetThreadAsync(It.IsAny<string>()))
            .ReturnsAsync(threadId);

        _mockAssistantService.Setup(x => x.GetAssistantReplyAsync(threadId, It.IsAny<string>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _extractionService.ExtractNameAsync(message, languageCode);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal(ExtractionMethod.Failed, result.Method);
        Assert.Equal("LLM could not find name", result.ErrorMessage);
    }
}