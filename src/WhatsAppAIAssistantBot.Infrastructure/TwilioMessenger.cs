using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WhatsAppAIAssistantBot.Infrastructure;

public class TwilioMessenger : ITwilioMessenger
{
    private readonly ILogger<TwilioMessenger> _logger;
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromNumber;
    private readonly bool _isInitialized;

    public TwilioMessenger(IConfiguration configuration, ILogger<TwilioMessenger> logger)
    {
        _logger = logger;
        
        try
        {
            _accountSid = configuration["Twilio:AccountSid"];
            if (string.IsNullOrWhiteSpace(_accountSid))
                throw new InvalidOperationException("Twilio Account SID is not configured.");
            
            _authToken = configuration["Twilio:AuthToken"];
            if (string.IsNullOrWhiteSpace(_authToken))
                throw new InvalidOperationException("Twilio Auth Token is not configured.");
            
            _fromNumber = configuration["Twilio:FromNumber"];
            if (string.IsNullOrWhiteSpace(_fromNumber))
                throw new InvalidOperationException("Twilio From Number is not configured.");
            
            TwilioClient.Init(_accountSid, _authToken);
            _isInitialized = true;
            
            _logger.LogInformation("TwilioMessenger initialized successfully with From number: {FromNumber}", _fromNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize TwilioMessenger");
            _isInitialized = false;
            throw;
        }
    }

    public async Task SendMessageAsync(string to, string message)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("TwilioMessenger is not properly initialized.");
        }

        _logger.LogInformation("Sending WhatsApp message to {To}, length: {MessageLength}", to, message?.Length ?? 0);
        
        try
        {
            var messageResource = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(_fromNumber),
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