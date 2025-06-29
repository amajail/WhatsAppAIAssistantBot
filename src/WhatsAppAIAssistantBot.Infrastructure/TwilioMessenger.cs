using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WhatsAppAIAssistantBot.Infrastructure;

public class TwilioMessenger(IConfiguration configuration, ILogger<TwilioMessenger> logger) : ITwilioMessenger
{
    private readonly ILogger<TwilioMessenger> _logger = logger;

    public async Task SendMessageAsync(string to, string message)
    {
        _logger.LogInformation("Sending WhatsApp message to {To}, length: {MessageLength}", to, message?.Length ?? 0);
        
        try
        {
            if (configuration == null)
                throw new InvalidOperationException("TwilioMessenger not initialized.");

            var accountSid = configuration["Twilio:AccountSid"] ?? throw new InvalidOperationException("Twilio Account SID is not configured.");
            var authToken = configuration["Twilio:AuthToken"] ?? throw new InvalidOperationException("Twilio Auth Token is not configured.");
            var fromNumber = configuration["Twilio:FromNumber"] ?? throw new InvalidOperationException("Twilio From Number is not configured.");

            _logger.LogDebug("Initializing Twilio client for message to {To} from {From}", to, fromNumber);
            
            TwilioClient.Init(accountSid, authToken);

            var messageResource = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(fromNumber),
                to: new PhoneNumber(to)
            );

            _logger.LogInformation("WhatsApp message sent successfully to {To}, Twilio SID: {MessageSid}, Status: {Status}", 
                to, messageResource.Sid, messageResource.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp message to {To}", to);
            throw;
        }
    }
}

public interface ITwilioMessenger
{
    Task SendMessageAsync(string to, string message);
}