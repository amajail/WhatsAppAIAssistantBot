using WhatsAppAIAssistantBot.Application;
using WhatsAppAIAssistantBot.Infrastructure;
using WhatsAppAIAssistantBot.Domain.Services;
using WhatsAppAIAssistantBot.Domain.Entities;
using Moq;
using Xunit;

namespace WhatsAppAIAssistantBot.Tests;

public class OrchestrationServiceTests
{
    private readonly Mock<ISemanticKernelService> _mockSemanticKernel;
    private readonly Mock<IAssistantService> _mockAssistant;
    private readonly Mock<ITwilioMessenger> _mockTwilioMessenger;
    private readonly Mock<IUserStorageService> _mockUserStorageService;
    private readonly Mock<ILocalizationService> _mockLocalizationService;
    private readonly OrchestrationService _orchestrationService;

    public OrchestrationServiceTests()
    {
        _mockSemanticKernel = new Mock<ISemanticKernelService>();
        _mockAssistant = new Mock<IAssistantService>();
        _mockTwilioMessenger = new Mock<ITwilioMessenger>();
        _mockUserStorageService = new Mock<IUserStorageService>();
        _mockLocalizationService = new Mock<ILocalizationService>();
        
        _orchestrationService = new OrchestrationService(
            _mockSemanticKernel.Object,
            _mockAssistant.Object,
            _mockTwilioMessenger.Object,
            _mockUserStorageService.Object,
            _mockLocalizationService.Object
        );
    }

    [Fact]
    public async Task HandleMessageAsync_WithRegisteredUser_ShouldCreateThreadAndSendReply()
    {
        // Arrange
        var userId = "whatsapp:+1234567890";
        var message = "Hello, how are you?";
        var threadId = "thread_123";
        var reply = "I'm doing well, thank you!";
        var registeredUser = new User
        {
            PhoneNumber = userId,
            ThreadId = threadId,
            Name = "John Doe",
            Email = "john@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockAssistant.Setup(x => x.CreateOrGetThreadAsync(userId))
            .ReturnsAsync(threadId);
        
        _mockUserStorageService.Setup(x => x.GetUserByPhoneNumberAsync(userId))
            .ReturnsAsync(registeredUser);
        
        _mockAssistant.Setup(x => x.GetAssistantReplyAsync(threadId, message))
            .ReturnsAsync(reply);

        // Act
        await _orchestrationService.HandleMessageAsync(userId, message);

        // Assert
        _mockAssistant.Verify(x => x.CreateOrGetThreadAsync(userId), Times.Once);
        _mockUserStorageService.Verify(x => x.GetUserByPhoneNumberAsync(userId), Times.Once);
        _mockAssistant.Verify(x => x.GetAssistantReplyAsync(threadId, message), Times.Once);
        _mockTwilioMessenger.Verify(x => x.SendMessageAsync(userId, reply), Times.Once);
    }

    [Fact]
    public async Task HandleMessageAsync_WithNewUser_ShouldCreateUserAndRequestName()
    {
        // Arrange
        var userId = "whatsapp:+1234567890";
        var message = "Hello";
        var threadId = "thread_123";
        var newUser = new User
        {
            PhoneNumber = userId,
            ThreadId = threadId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockAssistant.Setup(x => x.CreateOrGetThreadAsync(userId))
            .ReturnsAsync(threadId);
        
        _mockUserStorageService.Setup(x => x.GetUserByPhoneNumberAsync(userId))
            .ReturnsAsync(newUser);

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.WelcomeMessage, "es"))
            .ReturnsAsync("¡Bienvenido! Por favor dime tu nombre");

        // Act
        await _orchestrationService.HandleMessageAsync(userId, message);

        // Assert
        _mockAssistant.Verify(x => x.CreateOrGetThreadAsync(userId), Times.Once);
        _mockUserStorageService.Verify(x => x.GetUserByPhoneNumberAsync(userId), Times.Once);
        _mockTwilioMessenger.Verify(x => x.SendMessageAsync(userId, 
            "¡Bienvenido! Por favor dime tu nombre"), Times.Once);
    }

    [Fact]
    public async Task HandleMessageAsync_WithNameInput_ShouldUpdateUserAndRequestEmail()
    {
        // Arrange
        var userId = "whatsapp:+1234567890";
        var message = "Name: John Doe";
        var threadId = "thread_123";
        var unregisteredUser = new User
        {
            PhoneNumber = userId,
            ThreadId = threadId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockUserStorageService.Setup(x => x.GetUserByPhoneNumberAsync(userId))
            .ReturnsAsync(unregisteredUser);

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.GreetWithName, "es", "John Doe"))
            .ReturnsAsync("¡Hola John Doe! Por favor proporciona tu correo electrónico");

        // Act
        await _orchestrationService.HandleMessageAsync(userId, message);

        // Assert
        _mockUserStorageService.Verify(x => x.UpdateUserRegistrationAsync(userId, "John Doe", string.Empty), Times.Once);
        _mockTwilioMessenger.Verify(x => x.SendMessageAsync(userId, 
            "¡Hola John Doe! Por favor proporciona tu correo electrónico"), Times.Once);
    }

    [Fact]
    public async Task HandleMessageAsync_WithEmailInput_ShouldCompleteRegistration()
    {
        // Arrange
        var userId = "whatsapp:+1234567890";
        var message = "Email: john@example.com";
        var threadId = "thread_123";
        var userWithName = new User
        {
            PhoneNumber = userId,
            ThreadId = threadId,
            Name = "John Doe",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockUserStorageService.Setup(x => x.GetUserByPhoneNumberAsync(userId))
            .ReturnsAsync(userWithName);

        _mockLocalizationService.Setup(x => x.GetLocalizedMessageAsync(
            LocalizationKeys.RegistrationComplete, "es", "John Doe"))
            .ReturnsAsync("¡Gracias John Doe! Tu registro está completo. ¿Cómo puedo ayudarte hoy?");

        // Act
        await _orchestrationService.HandleMessageAsync(userId, message);

        // Assert
        _mockUserStorageService.Verify(x => x.UpdateUserRegistrationAsync(userId, "John Doe", "john@example.com"), Times.Once);
        _mockTwilioMessenger.Verify(x => x.SendMessageAsync(userId, 
            "¡Gracias John Doe! Tu registro está completo. ¿Cómo puedo ayudarte hoy?"), Times.Once);
    }
}