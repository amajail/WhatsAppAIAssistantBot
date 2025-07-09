using Microsoft.Extensions.Logging;
using WhatsAppAIAssistantBot.Application.Services;
using WhatsAppAIAssistantBot.Domain.Entities;
using WhatsAppAIAssistantBot.Domain.Models;
using WhatsAppAIAssistantBot.Domain.Services;
using WhatsAppAIAssistantBot.Domain.Services.Calendar;
using WhatsAppAIAssistantBot.Infrastructure;
using Moq;
using Xunit;

namespace WhatsAppAIAssistantBot.Tests;

public class CommandHandlerServiceTests
{
    private readonly Mock<ILocalizationService> _mockLocalizationService;
    private readonly Mock<IUserStorageService> _mockUserStorageService;
    private readonly Mock<ITwilioMessenger> _mockTwilioMessenger;
    private readonly Mock<IGoogleCalendarService> _mockGoogleCalendarService;
    private readonly Mock<ILogger<CommandHandlerService>> _mockLogger;
    private readonly CommandHandlerService _commandHandlerService;
    private readonly User _testUser;

    public CommandHandlerServiceTests()
    {
        _mockLocalizationService = new Mock<ILocalizationService>();
        _mockUserStorageService = new Mock<IUserStorageService>();
        _mockTwilioMessenger = new Mock<ITwilioMessenger>();
        _mockGoogleCalendarService = new Mock<IGoogleCalendarService>();
        _mockLogger = new Mock<ILogger<CommandHandlerService>>();
        
        _commandHandlerService = new CommandHandlerService(
            _mockLocalizationService.Object,
            _mockUserStorageService.Object,
            _mockTwilioMessenger.Object,
            _mockGoogleCalendarService.Object,
            _mockLogger.Object
        );

        _testUser = new User
        {
            PhoneNumber = "whatsapp:+1234567890",
            LanguageCode = "es",
            Name = "Test User",
            Email = "test@example.com"
        };
    }

    [Theory]
    [InlineData("/lang en")]
    [InlineData("/LANG EN")]
    [InlineData("/idioma en")]
    [InlineData("/IDIOMA EN")]
    public async Task HandleCommandAsync_WithValidLanguageCommand_ShouldReturnTrueAndUpdateLanguage(string command)
    {
        // Arrange
        _mockLocalizationService.Setup(x => x.IsLanguageSupportedAsync("en"))
            .ReturnsAsync(true);
        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.LanguageChanged, "en", "English"))
            .ReturnsAsync("Language changed to English");

        // Act
        var result = await _commandHandlerService.HandleCommandAsync(_testUser, command);

        // Assert
        Assert.True(result);
        Assert.Equal("en", _testUser.LanguageCode);
        _mockUserStorageService.Verify(x => x.CreateOrUpdateUserAsync(_testUser), Times.Once);
        _mockTwilioMessenger.Verify(x => x.SendMessageAsync(_testUser.PhoneNumber, 
            "Language changed to English"), Times.Once);
    }

    [Theory]
    [InlineData("/lang")]
    [InlineData("/idioma")]
    [InlineData("/lang ")]
    [InlineData("/idioma ")]
    public async Task HandleCommandAsync_WithIncompleteLanguageCommand_ShouldReturnFalse(string command)
    {
        // Act
        var result = await _commandHandlerService.HandleCommandAsync(_testUser, command);

        // Assert
        Assert.False(result);
        _mockUserStorageService.Verify(x => x.CreateOrUpdateUserAsync(It.IsAny<User>()), Times.Never);
        _mockTwilioMessenger.Verify(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("/lang xyz")]
    [InlineData("/idioma invalid")]
    public async Task HandleCommandAsync_WithUnsupportedLanguage_ShouldReturnTrueAndSendError(string command)
    {
        // Arrange
        _mockLocalizationService.Setup(x => x.IsLanguageSupportedAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.LanguageNotSupported, _testUser.LanguageCode))
            .ReturnsAsync("Language not supported");

        // Act
        var result = await _commandHandlerService.HandleCommandAsync(_testUser, command);

        // Assert
        Assert.True(result);
        _mockUserStorageService.Verify(x => x.CreateOrUpdateUserAsync(It.IsAny<User>()), Times.Never);
        _mockTwilioMessenger.Verify(x => x.SendMessageAsync(_testUser.PhoneNumber, 
            "Language not supported"), Times.Once);
    }

    [Theory]
    [InlineData("/help")]
    [InlineData("/HELP")]
    [InlineData("/ayuda")]
    [InlineData("/AYUDA")]
    public async Task HandleCommandAsync_WithHelpCommand_ShouldReturnTrueAndSendHelp(string command)
    {
        // Arrange
        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.HelpMessage, _testUser.LanguageCode))
            .ReturnsAsync("Help message in Spanish");

        // Act
        var result = await _commandHandlerService.HandleCommandAsync(_testUser, command);

        // Assert
        Assert.True(result);
        _mockTwilioMessenger.Verify(x => x.SendMessageAsync(_testUser.PhoneNumber, 
            "Help message in Spanish"), Times.Once);
    }

    [Theory]
    [InlineData("hello world")]
    [InlineData("how are you?")]
    [InlineData("/unknown")]
    [InlineData("/help me")]
    [InlineData("lang en")]
    [InlineData("")]
    public async Task HandleCommandAsync_WithNonCommand_ShouldReturnFalse(string message)
    {
        // Act
        var result = await _commandHandlerService.HandleCommandAsync(_testUser, message);

        // Assert
        Assert.False(result);
        _mockUserStorageService.Verify(x => x.CreateOrUpdateUserAsync(It.IsAny<User>()), Times.Never);
        _mockTwilioMessenger.Verify(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleCommandAsync_WithNullMessage_ShouldReturnFalse()
    {
        // Act
        var result = await _commandHandlerService.HandleCommandAsync(_testUser, null!);

        // Assert
        Assert.False(result);
        _mockUserStorageService.Verify(x => x.CreateOrUpdateUserAsync(It.IsAny<User>()), Times.Never);
        _mockTwilioMessenger.Verify(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleCommandAsync_LanguageCommand_WithSpaces_ShouldHandleCorrectly()
    {
        // Arrange
        var command = "  /lang   en  ";
        _mockLocalizationService.Setup(x => x.IsLanguageSupportedAsync("en"))
            .ReturnsAsync(true);
        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.LanguageChanged, "en", "English"))
            .ReturnsAsync("Language changed to English");

        // Act
        var result = await _commandHandlerService.HandleCommandAsync(_testUser, command);

        // Assert
        Assert.True(result);
        Assert.Equal("en", _testUser.LanguageCode);
    }

    [Fact]
    public async Task HandleCommandAsync_MultipleLanguageParameters_ShouldUseFirst()
    {
        // Arrange  
        var command = "/lang en es fr";
        _mockLocalizationService.Setup(x => x.IsLanguageSupportedAsync("en"))
            .ReturnsAsync(true);
        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.LanguageChanged, "en", "English"))
            .ReturnsAsync("Language changed to English");

        // Act
        var result = await _commandHandlerService.HandleCommandAsync(_testUser, command);

        // Assert
        Assert.True(result);
        Assert.Equal("en", _testUser.LanguageCode);
        _mockUserStorageService.Verify(x => x.CreateOrUpdateUserAsync(_testUser), Times.Once);
    }

    [Theory]
    [InlineData("/lang es")]
    [InlineData("/idioma es")]
    public async Task HandleCommandAsync_SupportedLanguage_ShouldUpdateUserLanguage(string command)
    {
        // Arrange
        var originalLanguage = _testUser.LanguageCode;
        _mockLocalizationService.Setup(x => x.IsLanguageSupportedAsync("es"))
            .ReturnsAsync(true);
        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.LanguageChanged, "es", "Español"))
            .ReturnsAsync("Idioma cambiado a Español");

        // Act
        var result = await _commandHandlerService.HandleCommandAsync(_testUser, command);

        // Assert
        Assert.True(result);
        Assert.Equal("es", _testUser.LanguageCode);
        _mockUserStorageService.Verify(x => x.CreateOrUpdateUserAsync(_testUser), Times.Once);
        _mockTwilioMessenger.Verify(x => x.SendMessageAsync(_testUser.PhoneNumber, 
            "Idioma cambiado a Español"), Times.Once);
    }
}