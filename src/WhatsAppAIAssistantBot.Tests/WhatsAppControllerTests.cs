using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WhatsAppAIAssistantBot.Api.Controllers;
using WhatsAppAIAssistantBot.Application;
using WhatsAppAIAssistantBot.Models;
using Moq;
using Xunit;

namespace WhatsAppAIAssistantBot.Tests;

public class WhatsAppControllerTests
{
    private readonly Mock<IOrchestrationService> _mockOrchestrationService;
    private readonly Mock<ILogger<WhatsAppController>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly WhatsAppController _controller;

    public WhatsAppControllerTests()
    {
        _mockOrchestrationService = new Mock<IOrchestrationService>();
        _mockLogger = new Mock<ILogger<WhatsAppController>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _controller = new WhatsAppController(_mockOrchestrationService.Object, _mockLogger.Object, _mockConfiguration.Object);
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
}