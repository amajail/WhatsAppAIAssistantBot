using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WhatsAppAIAssistantBot.Infrastructure.Services;
using Moq;
using Xunit;

namespace WhatsAppAIAssistantBot.Tests;

/// <summary>
/// Unit tests for the WebhookSecurityService to verify signature validation and message sanitization functionality.
/// </summary>
public class WebhookSecurityServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<WebhookSecurityService>> _mockLogger;
    private readonly WebhookSecurityService _securityService;

    public WebhookSecurityServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<WebhookSecurityService>>();
        
        // Setup default configuration
        _mockConfiguration.Setup(x => x["Twilio:AuthToken"]).Returns("test-auth-token");
        _mockConfiguration.Setup(x => x["Twilio:ValidateSignature"]).Returns("true");
        
        _securityService = new WebhookSecurityService(_mockConfiguration.Object, _mockLogger.Object);
    }

    [Fact]
    public void ShouldValidateSignature_WithConfigDisabled_ShouldReturnFalse()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["Twilio:ValidateSignature"]).Returns("false");
        var securityService = new WebhookSecurityService(_mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = securityService.ShouldValidateSignature();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldValidateSignature_WithConfigEnabled_ShouldReturnTrue()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["Twilio:ValidateSignature"]).Returns("true");
        var securityService = new WebhookSecurityService(_mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = securityService.ShouldValidateSignature();

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void SanitizeMessage_WithEmptyOrNullInput_ShouldReturnEmpty(string input)
    {
        // Act
        var result = _securityService.SanitizeMessage(input);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SanitizeMessage_WithValidMessage_ShouldTrimWhitespace()
    {
        // Arrange
        var input = "  Hello World  ";

        // Act
        var result = _securityService.SanitizeMessage(input);

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void SanitizeMessage_WithExcessiveWhitespace_ShouldNormalize()
    {
        // Arrange
        var input = "Hello    World\t\t\nTest";

        // Act
        var result = _securityService.SanitizeMessage(input);

        // Assert
        Assert.Equal("Hello World Test", result);
    }

    [Fact]
    public void SanitizeMessage_WithLongMessage_ShouldTruncate()
    {
        // Arrange
        var input = new string('A', 5000); // 5000 characters

        // Act
        var result = _securityService.SanitizeMessage(input);

        // Assert
        Assert.Equal(4000, result.Length);
        Assert.All(result, c => Assert.Equal('A', c));
    }

    [Fact]
    public void SanitizeMessage_WithNullBytes_ShouldRemoveThem()
    {
        // Arrange
        var input = "Hello\0World\0Test";

        // Act
        var result = _securityService.SanitizeMessage(input);

        // Assert
        Assert.Equal("HelloWorldTest", result);
    }

    [Fact]
    public void SanitizeMessage_WithMixedLineEndings_ShouldNormalize()
    {
        // Arrange
        var input = "Line1\r\nLine2\rLine3\nLine4";

        // Act
        var result = _securityService.SanitizeMessage(input);

        // Assert
        Assert.Equal("Line1 Line2 Line3 Line4", result);
    }

    [Fact]
    public void ValidateSignature_WithMissingAuthToken_ShouldReturnFalse()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["Twilio:AuthToken"]).Returns((string?)null);
        var securityService = new WebhookSecurityService(_mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = securityService.ValidateSignature("signature", "url", new List<KeyValuePair<string, string>>());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateSignature_WithEmptySignature_ShouldReturnFalse()
    {
        // Act
        var result = _securityService.ValidateSignature("", "url", new List<KeyValuePair<string, string>>());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateSignature_WithValidSignature_ShouldReturnTrue()
    {
        // Arrange - This test uses a known valid signature calculation
        var url = "https://mycompany.com/myapp.php?foo=1&bar=2";
        var formData = new List<KeyValuePair<string, string>>
        {
            new("Digits", "1234"),
            new("To", "+18005551212"),
            new("From", "+14158675309"),
            new("Caller", "+14158675309"),
            new("CallSid", "CA1234567890ABCDE")
        };
        // This is the expected signature for the above data with auth token "12345"
        _mockConfiguration.Setup(x => x["Twilio:AuthToken"]).Returns("12345");
        var securityService = new WebhookSecurityService(_mockConfiguration.Object, _mockLogger.Object);
        // Calculate the expected signature using the same logic as the service
        var dataToSign = url + string.Concat(formData.OrderBy(x => x.Key).Select(x => x.Key + x.Value));
        using var hmac = new System.Security.Cryptography.HMACSHA1(System.Text.Encoding.UTF8.GetBytes("12345"));
        var expectedSignature = System.Convert.ToBase64String(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(dataToSign)));

        // Act
        var result = securityService.ValidateSignature(expectedSignature, url, formData);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateSignature_WithInvalidSignature_ShouldReturnFalse()
    {
        // Arrange
        var url = "https://example.com/webhook";
        var formData = new List<KeyValuePair<string, string>>
        {
            new("From", "whatsapp:+1234567890"),
            new("Body", "Test message")
        };
        var invalidSignature = "InvalidSignature123";

        // Act
        var result = _securityService.ValidateSignature(invalidSignature, url, formData);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateSignature_WithException_ShouldReturnFalse()
    {
        // Arrange - Setup configuration to throw exception
        _mockConfiguration.Setup(x => x["Twilio:AuthToken"]).Throws(new Exception("Test exception"));
        var securityService = new WebhookSecurityService(_mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = securityService.ValidateSignature("signature", "url", new List<KeyValuePair<string, string>>());

        // Assert
        Assert.False(result);
    }
}