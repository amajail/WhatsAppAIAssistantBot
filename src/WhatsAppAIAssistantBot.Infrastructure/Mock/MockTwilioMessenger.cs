using Microsoft.Extensions.Logging;

namespace WhatsAppAIAssistantBot.Infrastructure.Mock;

public class MockTwilioMessenger : ITwilioMessenger
{
    private readonly ILogger<MockTwilioMessenger> _logger;

    public MockTwilioMessenger(ILogger<MockTwilioMessenger> logger)
    {
        _logger = logger;
        _logger.LogInformation("MockTwilioMessenger initialized for development mode");
    }

    public Task SendMessageAsync(string to, string message)
    {
        _logger.LogInformation("📱 MOCK WhatsApp Message 📱{NewLine}To: {To}{NewLine}Message: {Message}{NewLine}──────────────────────────────", 
            Environment.NewLine, to, Environment.NewLine, message, Environment.NewLine);
        
        return Task.CompletedTask;
    }
}