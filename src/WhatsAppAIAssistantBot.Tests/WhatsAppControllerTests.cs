using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;
using WhatsAppAIAssistantBot.Api.Controllers;
using WhatsAppAIAssistantBot.Application;
using WhatsAppAIAssistantBot.Models;
using WhatsAppAIAssistantBot.Domain.Services;
using Moq;
using Xunit;

namespace WhatsAppAIAssistantBot.Tests;

public class WhatsAppControllerTests
{
    private readonly Mock<IOrchestrationService> _mockOrchestrationService;
    private readonly Mock<ILogger<WhatsAppController>> _mockLogger;
    private readonly Mock<IWebhookSecurityService> _mockSecurityService;
    private readonly WhatsAppController _controller;

    public WhatsAppControllerTests()
    {
        _mockOrchestrationService = new Mock<IOrchestrationService>();
        _mockLogger = new Mock<ILogger<WhatsAppController>>();
        _mockSecurityService = new Mock<IWebhookSecurityService>();
        
        // Setup security service to skip signature validation in tests
        _mockSecurityService.Setup(x => x.ShouldValidateSignature()).Returns(false);
        _mockSecurityService.Setup(x => x.SanitizeMessage(It.IsAny<string>())).Returns<string>(x => x);
        
        _controller = new WhatsAppController(_mockOrchestrationService.Object, _mockLogger.Object, _mockSecurityService.Object);
    }

    [Fact]
    public async Task Receive_WithNullInput_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.Receive(null!);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = badRequestResult.Value;
        Assert.NotNull(response);
        
        // Verify orchestrator was never called
        _mockOrchestrationService.Verify(x => x.HandleMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Receive_WithNullFromField_ShouldReturnBadRequest()
    {
        // Arrange
        var input = new TwilioWebhookModel { From = null!, Body = "Test message" };

        // Act
        var result = await _controller.Receive(input);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = badRequestResult.Value;
        Assert.NotNull(response);
        
        // Verify orchestrator was never called
        _mockOrchestrationService.Verify(x => x.HandleMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Receive_WithEmptyFromField_ShouldReturnBadRequest()
    {
        // Arrange
        var input = new TwilioWebhookModel { From = "", Body = "Test message" };

        // Act
        var result = await _controller.Receive(input);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = badRequestResult.Value;
        Assert.NotNull(response);
        
        // Verify orchestrator was never called
        _mockOrchestrationService.Verify(x => x.HandleMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Receive_WithWhitespaceOnlyFromField_ShouldReturnBadRequest()
    {
        // Arrange
        var input = new TwilioWebhookModel { From = "   ", Body = "Test message" };

        // Act
        var result = await _controller.Receive(input);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = badRequestResult.Value;
        Assert.NotNull(response);
        
        // Verify orchestrator was never called
        _mockOrchestrationService.Verify(x => x.HandleMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Receive_WithNullBody_ShouldReturnOkAndLogInfo()
    {
        // Arrange
        var input = new TwilioWebhookModel { From = "whatsapp:+1234567890", Body = null! };

        // Act
        var result = await _controller.Receive(input);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        
        // Verify orchestrator was never called for null body
        _mockOrchestrationService.Verify(x => x.HandleMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Receive_WithEmptyBody_ShouldReturnOkAndLogInfo()
    {
        // Arrange
        var input = new TwilioWebhookModel { From = "whatsapp:+1234567890", Body = "" };

        // Act
        var result = await _controller.Receive(input);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        
        // Verify orchestrator was never called for empty body
        _mockOrchestrationService.Verify(x => x.HandleMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Receive_WithValidInput_ShouldCallOrchestratorAndReturnOk()
    {
        // Arrange
        var input = new TwilioWebhookModel 
        { 
            From = "whatsapp:+1234567890", 
            Body = "Hello, world!" 
        };

        _mockOrchestrationService
            .Setup(x => x.HandleMessageAsync(input.From, input.Body))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Receive(input);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        
        // Verify orchestrator was called with correct parameters
        _mockOrchestrationService.Verify(x => x.HandleMessageAsync(input.From, input.Body), Times.Once);
    }

    [Fact]
    public async Task Receive_WhenOrchestratorThrowsException_ShouldReturn500WithGenericMessage()
    {
        // Arrange
        var input = new TwilioWebhookModel 
        { 
            From = "whatsapp:+1234567890", 
            Body = "Hello, world!" 
        };

        var sensitiveException = new InvalidOperationException("Database connection string: Server=sensitive-db;Password=secret123");
        _mockOrchestrationService
            .Setup(x => x.HandleMessageAsync(input.From, input.Body))
            .ThrowsAsync(sensitiveException);

        // Act
        var result = await _controller.Receive(input);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        
        var response = statusCodeResult.Value;
        Assert.NotNull(response);
        
        // Verify the response contains generic error message only
        var responseJson = System.Text.Json.JsonSerializer.Serialize(response);
        Assert.Contains("Internal server error occurred while processing your message", responseJson);
        
        // Verify sensitive information is NOT exposed
        Assert.DoesNotContain("Database connection string", responseJson);
        Assert.DoesNotContain("secret123", responseJson);
        Assert.DoesNotContain("sensitive-db", responseJson);
    }

    [Fact]
    public async Task Receive_WhenOrchestratorThrowsException_ShouldLogErrorWithDetails()
    {
        // Arrange
        var input = new TwilioWebhookModel 
        { 
            From = "whatsapp:+1234567890", 
            Body = "Hello, world!" 
        };

        var exception = new InvalidOperationException("Test exception");
        _mockOrchestrationService
            .Setup(x => x.HandleMessageAsync(input.From, input.Body))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.Receive(input);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        
        // Verify error logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error processing message from")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Receive_WhenOrchestratorThrowsException_ShouldNotExposeStackTrace()
    {
        // Arrange
        var input = new TwilioWebhookModel 
        { 
            From = "whatsapp:+1234567890", 
            Body = "Hello, world!" 
        };

        var exception = new InvalidOperationException("Test exception");
        _mockOrchestrationService
            .Setup(x => x.HandleMessageAsync(input.From, input.Body))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.Receive(input);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        var responseJson = System.Text.Json.JsonSerializer.Serialize(statusCodeResult.Value);
        
        // Verify stack trace is not exposed
        Assert.DoesNotContain("at ", responseJson); // Stack trace typically contains "at ..."
        Assert.DoesNotContain("System.", responseJson); // Avoid exposing system internals
        Assert.DoesNotContain("Exception", responseJson); // Don't expose exception type names
    }

    [Fact]
    public async Task Receive_ErrorScenarios_ShouldAlwaysReturnConsistentErrorFormat()
    {
        // Arrange
        var input = new TwilioWebhookModel 
        { 
            From = "whatsapp:+1234567890", 
            Body = "Hello, world!" 
        };

        var exception = new Exception("Any exception");
        _mockOrchestrationService
            .Setup(x => x.HandleMessageAsync(input.From, input.Body))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.Receive(input);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        var responseJson = System.Text.Json.JsonSerializer.Serialize(statusCodeResult.Value);
        
        // Verify consistent error format
        Assert.Contains("error", responseJson);
        Assert.Contains("Internal server error occurred while processing your message", responseJson);
        
        // Verify it's a proper JSON object (not just a string)
        Assert.StartsWith("{", responseJson.Trim());
        Assert.EndsWith("}", responseJson.Trim());
    }

    [Fact]
    public async Task Receive_WithSignatureValidationEnabled_AndInvalidSignature_ShouldReturnUnauthorized()
    {
        // Arrange
        var mockOrchestrationService = new Mock<IOrchestrationService>();
        var mockLogger = new Mock<ILogger<WhatsAppController>>();
        var mockSecurityService = new Mock<IWebhookSecurityService>();
        
        // Setup security service to enable validation and return false (invalid signature)
        mockSecurityService.Setup(x => x.ShouldValidateSignature()).Returns(true);
        mockSecurityService.Setup(x => x.ValidateSignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
            .Returns(false);
        mockSecurityService.Setup(x => x.SanitizeMessage(It.IsAny<string>())).Returns<string>(x => x);
        
        var controller = new WhatsAppController(mockOrchestrationService.Object, mockLogger.Object, mockSecurityService.Object);
        
        // Setup ControllerContext and HttpContext with required headers and form
        var input = new TwilioWebhookModel { From = "whatsapp:+1234567890", Body = "Test message" };
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Twilio-Signature"] = "dummy";
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost");
        httpContext.Request.Path = "/api/whatsapp";
        httpContext.Request.QueryString = QueryString.Empty;
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            { "From", input.From },
            { "Body", input.Body }
        });
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await controller.Receive(input);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = unauthorizedResult.Value;
        Assert.NotNull(response);
        
        // Verify orchestrator was never called due to security validation failure
        mockOrchestrationService.Verify(x => x.HandleMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        
        // Verify security service was called
        mockSecurityService.Verify(x => x.ShouldValidateSignature(), Times.Once);
        mockSecurityService.Verify(x => x.ValidateSignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>()), Times.Once);
    }

    [Fact] 
    public async Task Receive_WithValidSignature_ShouldSanitizeMessageAndProcess()
    {
        // Arrange
        var mockOrchestrationService = new Mock<IOrchestrationService>();
        var mockLogger = new Mock<ILogger<WhatsAppController>>();
        var mockSecurityService = new Mock<IWebhookSecurityService>();
        mockSecurityService.Setup(x => x.ShouldValidateSignature()).Returns(true);
        mockSecurityService.Setup(x => x.ValidateSignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>())).Returns(true);
        mockSecurityService.Setup(x => x.SanitizeMessage("  Test message  ")).Returns("Test message");
        var controller = new WhatsAppController(mockOrchestrationService.Object, mockLogger.Object, mockSecurityService.Object);
        var input = new TwilioWebhookModel { From = "whatsapp:+1234567890", Body = "  Test message  " };
        // Setup ControllerContext and HttpContext with required headers and form
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Twilio-Signature"] = "dummy";
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost");
        httpContext.Request.Path = "/api/whatsapp";
        httpContext.Request.QueryString = QueryString.Empty;
        httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            { "From", input.From },
            { "Body", input.Body }
        });
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        // Act
        var result = await controller.Receive(input);
        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        mockSecurityService.Verify(x => x.ShouldValidateSignature(), Times.Once);
        mockSecurityService.Verify(x => x.ValidateSignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>()), Times.Once);
        mockSecurityService.Verify(x => x.SanitizeMessage("  Test message  "), Times.Once);
        mockOrchestrationService.Verify(x => x.HandleMessageAsync("whatsapp:+1234567890", "Test message"), Times.Once);
    }
}