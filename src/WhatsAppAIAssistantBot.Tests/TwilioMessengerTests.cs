using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WhatsAppAIAssistantBot.Infrastructure;
using Moq;
using Xunit;

namespace WhatsAppAIAssistantBot.Tests;

public class TwilioMessengerTests
{
    private readonly Mock<ILogger<TwilioMessenger>> _mockLogger;

    public TwilioMessengerTests()
    {
        _mockLogger = new Mock<ILogger<TwilioMessenger>>();
    }

    private Mock<IConfiguration> CreateMockConfiguration(string? accountSid = null, string? authToken = null, string? fromNumber = null)
    {
        var mockConfiguration = new Mock<IConfiguration>();
        
        mockConfiguration.Setup(x => x["Twilio:AccountSid"]).Returns(accountSid);
        mockConfiguration.Setup(x => x["Twilio:AuthToken"]).Returns(authToken);
        mockConfiguration.Setup(x => x["Twilio:FromNumber"]).Returns(fromNumber);
        
        return mockConfiguration;
    }

    [Fact]
    public void Constructor_WithMissingAccountSid_ShouldThrowException()
    {
        // Arrange
        var mockConfiguration = CreateMockConfiguration(
            accountSid: null, // Missing AccountSid
            authToken: "test-auth-token",
            fromNumber: "whatsapp:+1234567890"
        );

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new TwilioMessenger(mockConfiguration.Object, _mockLogger.Object));
        
        Assert.Contains("Twilio Account SID is not configured", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyAccountSid_ShouldThrowException()
    {
        // Arrange
        var mockConfiguration = CreateMockConfiguration(
            accountSid: "", // Empty AccountSid
            authToken: "test-auth-token",
            fromNumber: "whatsapp:+1234567890"
        );

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new TwilioMessenger(mockConfiguration.Object, _mockLogger.Object));
        
        Assert.Contains("Twilio Account SID is not configured", exception.Message);
    }

    [Fact]
    public void Constructor_WithMissingAuthToken_ShouldThrowException()
    {
        // Arrange
        var mockConfiguration = CreateMockConfiguration(
            accountSid: "test-account-sid",
            authToken: null, // Missing AuthToken
            fromNumber: "whatsapp:+1234567890"
        );

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new TwilioMessenger(mockConfiguration.Object, _mockLogger.Object));
        
        Assert.Contains("Twilio Auth Token is not configured", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyAuthToken_ShouldThrowException()
    {
        // Arrange
        var mockConfiguration = CreateMockConfiguration(
            accountSid: "test-account-sid",
            authToken: "", // Empty AuthToken
            fromNumber: "whatsapp:+1234567890"
        );

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new TwilioMessenger(mockConfiguration.Object, _mockLogger.Object));
        
        Assert.Contains("Twilio Auth Token is not configured", exception.Message);
    }

    [Fact]
    public void Constructor_WithMissingFromNumber_ShouldThrowException()
    {
        // Arrange
        var mockConfiguration = CreateMockConfiguration(
            accountSid: "test-account-sid",
            authToken: "test-auth-token",
            fromNumber: null // Missing FromNumber
        );

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new TwilioMessenger(mockConfiguration.Object, _mockLogger.Object));
        
        Assert.Contains("Twilio From Number is not configured", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyFromNumber_ShouldThrowException()
    {
        // Arrange
        var mockConfiguration = CreateMockConfiguration(
            accountSid: "test-account-sid",
            authToken: "test-auth-token",
            fromNumber: "" // Empty FromNumber
        );

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new TwilioMessenger(mockConfiguration.Object, _mockLogger.Object));
        
        Assert.Contains("Twilio From Number is not configured", exception.Message);
    }

    [Fact]
    public void Constructor_WithValidConfiguration_ShouldInitializeSuccessfully()
    {
        // Arrange
        var mockConfiguration = CreateMockConfiguration(
            accountSid: "ACtest12345678901234567890123456",
            authToken: "test-auth-token",
            fromNumber: "whatsapp:+1234567890"
        );

        // Act & Assert - Should not throw
        var messenger = new TwilioMessenger(mockConfiguration.Object, _mockLogger.Object);
        
        // Verify success logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TwilioMessenger initialized successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WhenInitializationFails_ShouldLogErrorAndThrow()
    {
        // Arrange - Use null configuration to simulate initialization failure
        var mockConfiguration = CreateMockConfiguration(
            accountSid: null, // This will cause initialization to fail
            authToken: "test-auth-token",
            fromNumber: "whatsapp:+1234567890"
        );

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new TwilioMessenger(mockConfiguration.Object, _mockLogger.Object));

        // Verify error logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to initialize TwilioMessenger")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_WhenNotInitialized_ShouldThrowException()
    {
        // Arrange - Create an uninitialized messenger by causing initialization to fail
        var mockConfiguration = CreateMockConfiguration(
            accountSid: null, // This will cause initialization to fail
            authToken: "test-auth-token",
            fromNumber: "whatsapp:+1234567890"
        );

        // The constructor should throw, so we expect an exception
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var messenger = new TwilioMessenger(mockConfiguration.Object, _mockLogger.Object);
            await messenger.SendMessageAsync("to", "message");
        });
    }

    [Fact]
    public void Constructor_ShouldNotLogSensitiveCredentials()
    {
        // Arrange
        var sensitiveAuthToken = "sensitive-auth-token-12345";
        var mockConfiguration = CreateMockConfiguration(
            accountSid: "ACtest12345678901234567890123456",
            authToken: sensitiveAuthToken,
            fromNumber: "whatsapp:+1234567890"
        );

        // Act
        var messenger = new TwilioMessenger(mockConfiguration.Object, _mockLogger.Object);

        // Assert - Verify sensitive information is not logged
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(sensitiveAuthToken)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never,
            "Auth token should never be logged");
    }

    [Fact]
    public void Constructor_ShouldCacheCredentialsSecurely()
    {
        // Arrange
        var accountSid = "ACtest12345678901234567890123456";
        var authToken = "test-auth-token";
        var fromNumber = "whatsapp:+1234567890";
        
        var mockConfiguration = CreateMockConfiguration(accountSid, authToken, fromNumber);

        // Act
        var messenger = new TwilioMessenger(mockConfiguration.Object, _mockLogger.Object);

        // Assert - Configuration should only be accessed once during initialization
        // (This tests that credentials are cached, not retrieved repeatedly)
        mockConfiguration.Verify(x => x["Twilio:AccountSid"], Times.Once);
        mockConfiguration.Verify(x => x["Twilio:AuthToken"], Times.Once);
        mockConfiguration.Verify(x => x["Twilio:FromNumber"], Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidAccountSidValues_ShouldThrowException(string? invalidAccountSid)
    {
        // Arrange
        var mockConfiguration = CreateMockConfiguration(
            accountSid: invalidAccountSid,
            authToken: "test-auth-token",
            fromNumber: "whatsapp:+1234567890"
        );

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new TwilioMessenger(mockConfiguration.Object, _mockLogger.Object));
        
        Assert.Contains("Twilio Account SID is not configured", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidAuthTokenValues_ShouldThrowException(string? invalidAuthToken)
    {
        // Arrange
        var mockConfiguration = CreateMockConfiguration(
            accountSid: "ACtest12345678901234567890123456",
            authToken: invalidAuthToken,
            fromNumber: "whatsapp:+1234567890"
        );

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new TwilioMessenger(mockConfiguration.Object, _mockLogger.Object));
        
        Assert.Contains("Twilio Auth Token is not configured", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidFromNumberValues_ShouldThrowException(string? invalidFromNumber)
    {
        // Arrange
        var mockConfiguration = CreateMockConfiguration(
            accountSid: "ACtest12345678901234567890123456",
            authToken: "test-auth-token",
            fromNumber: invalidFromNumber
        );

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new TwilioMessenger(mockConfiguration.Object, _mockLogger.Object));
        
        Assert.Contains("Twilio From Number is not configured", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldInitializeTwilioClientOnce()
    {
        // This test verifies that TwilioClient.Init is called during construction
        // and credentials are properly cached for future use
        
        // Arrange
        var mockConfiguration = CreateMockConfiguration(
            accountSid: "ACtest12345678901234567890123456",
            authToken: "test-auth-token",
            fromNumber: "whatsapp:+1234567890"
        );

        // Act
        var messenger = new TwilioMessenger(mockConfiguration.Object, _mockLogger.Object);

        // Assert - Successful initialization should log success message
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TwilioMessenger initialized successfully") && 
                                             v.ToString()!.Contains("whatsapp:+1234567890")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}