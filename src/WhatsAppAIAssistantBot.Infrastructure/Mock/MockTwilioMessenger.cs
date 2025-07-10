using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WhatsAppAIAssistantBot.Infrastructure.Mock;

public class MockTwilioMessenger : ITwilioMessenger
{
    private readonly ILogger<MockTwilioMessenger> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public MockTwilioMessenger(
        ILogger<MockTwilioMessenger> logger,
        IConfiguration configuration,
        HttpClient httpClient)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClient;
        _logger.LogInformation("MockTwilioMessenger initialized for development mode");
    }

    public async Task SendMessageAsync(string to, string message)
    {
        _logger.LogInformation("📱 MOCK WhatsApp Message 📱{NewLine}To: {To}{NewLine}Message: {Message}{NewLine}──────────────────────────────", 
            Environment.NewLine, to, Environment.NewLine, message, Environment.NewLine);
        
        // Send to console app webhook if configured
        var webhookUrl = _configuration["Development:ConsoleAppWebhookUrl"];
        if (!string.IsNullOrEmpty(webhookUrl))
        {
            await SendToConsoleAppWebhookAsync(to, message, webhookUrl);
        }
    }

    private async Task SendToConsoleAppWebhookAsync(string to, string message, string webhookUrl)
    {
        try
        {
            var formData = new List<KeyValuePair<string, string>>
            {
                new("From", _configuration["Twilio:FromNumber"] ?? "whatsapp:+15551234567"),
                new("To", to),
                new("Body", message),
                new("MessageSid", GenerateMessageSid()),
                new("AccountSid", "MOCK_ACCOUNT_SID"),
                new("NumMedia", "0")
            };

            var content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync(webhookUrl, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent message to console app webhook");
            }
            else
            {
                _logger.LogWarning("Failed to send message to console app webhook. Status: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to console app webhook");
        }
    }

    private static string GenerateMessageSid()
    {
        return $"SM{Guid.NewGuid().ToString("N")[..32]}";
    }
}