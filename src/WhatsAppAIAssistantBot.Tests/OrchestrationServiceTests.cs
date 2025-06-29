using WhatsAppAIAssistantBot.Application;
using WhatsAppAIAssistantBot.Infrastructure;
using Moq;
using Xunit;

namespace WhatsAppAIAssistantBot.Tests;

public class OrchestrationServiceTests
{
    private readonly Mock<ISemanticKernelService> _mockSemanticKernel;
    private readonly Mock<IAssistantService> _mockAssistant;
    private readonly Mock<ITwilioMessenger> _mockTwilioMessenger;
    private readonly OrchestrationService _orchestrationService;

    public OrchestrationServiceTests()
    {
        _mockSemanticKernel = new Mock<ISemanticKernelService>();
        _mockAssistant = new Mock<IAssistantService>();
        _mockTwilioMessenger = new Mock<ITwilioMessenger>();
        
        _orchestrationService = new OrchestrationService(
            _mockSemanticKernel.Object,
            _mockAssistant.Object,
            _mockTwilioMessenger.Object
        );
    }

    [Fact]
    public async Task HandleMessageAsync_ShouldCreateThreadAndSendReply()
    {
        // Arrange
        var userId = "whatsapp:+1234567890";
        var message = "Hello, how are you?";
        var threadId = "thread_123";
        var reply = "I'm doing well, thank you!";

        _mockAssistant.Setup(x => x.CreateOrGetThreadAsync(userId))
            .ReturnsAsync(threadId);
        
        _mockAssistant.Setup(x => x.GetAssistantReplyAsync(threadId, message))
            .ReturnsAsync(reply);

        // Act
        await _orchestrationService.HandleMessageAsync(userId, message);

        // Assert
        _mockAssistant.Verify(x => x.CreateOrGetThreadAsync(userId), Times.Once);
        _mockAssistant.Verify(x => x.GetAssistantReplyAsync(threadId, message), Times.Once);
        _mockTwilioMessenger.Verify(x => x.SendMessageAsync(userId, reply), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task HandleMessageAsync_WithEmptyMessage_ShouldStillProcessMessage(string message)
    {
        // Arrange
        var userId = "whatsapp:+1234567890";
        var threadId = "thread_123";
        var reply = "I didn't understand that.";

        _mockAssistant.Setup(x => x.CreateOrGetThreadAsync(userId))
            .ReturnsAsync(threadId);
        
        _mockAssistant.Setup(x => x.GetAssistantReplyAsync(threadId, message))
            .ReturnsAsync(reply);

        // Act
        await _orchestrationService.HandleMessageAsync(userId, message);

        // Assert
        _mockAssistant.Verify(x => x.CreateOrGetThreadAsync(userId), Times.Once);
        _mockTwilioMessenger.Verify(x => x.SendMessageAsync(userId, reply), Times.Once);
    }
}