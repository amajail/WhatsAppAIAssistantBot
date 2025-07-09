using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WhatsAppAIAssistantBot.Infrastructure;

/// <summary>
/// Implementation of WhatsApp messaging service using Twilio API.
/// Handles sending messages to WhatsApp users through Twilio's messaging platform.
/// </summary>
/// <remarks>
/// This service requires Twilio configuration including Account SID, Auth Token, and From Number.
/// The From Number must be a Twilio-registered WhatsApp number in the format "whatsapp:+1234567890".
/// </remarks>
public class TwilioMessenger : ITwilioMessenger
{
    private readonly ILogger<TwilioMessenger> _logger;
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromNumber;
    private readonly bool _isInitialized;

    /// <summary>
    /// Initializes a new instance of the TwilioMessenger with configuration and logging.
    /// </summary>
    /// <param name="configuration">Application configuration containing Twilio settings</param>
    /// <param name="logger">Logger instance for this service</param>
    /// <exception cref="InvalidOperationException">Thrown when required Twilio configuration is missing</exception>
    public TwilioMessenger(IConfiguration configuration, ILogger<TwilioMessenger> logger)
    {
        _logger = logger;
        
        try
        {
            _accountSid = configuration["Twilio:AccountSid"] ?? throw new InvalidOperationException("Twilio Account SID is not configured.");
            if (string.IsNullOrWhiteSpace(_accountSid))
                throw new InvalidOperationException("Twilio Account SID is not configured.");
            
            _authToken = configuration["Twilio:AuthToken"] ?? throw new InvalidOperationException("Twilio Auth Token is not configured.");
            if (string.IsNullOrWhiteSpace(_authToken))
                throw new InvalidOperationException("Twilio Auth Token is not configured.");
            
            _fromNumber = configuration["Twilio:FromNumber"] ?? throw new InvalidOperationException("Twilio From Number is not configured.");
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

    /// <summary>
    /// Sends a WhatsApp message to the specified recipient using Twilio API.
    /// </summary>
    /// <param name="to">The recipient's WhatsApp number (e.g., "whatsapp:+1234567890")</param>
    /// <param name="message">The message content to send</param>
    /// <returns>A task representing the asynchronous send operation</returns>
    /// <exception cref="InvalidOperationException">Thrown when TwilioMessenger is not properly initialized</exception>
    /// <exception cref="ArgumentException">Thrown when to or message parameters are invalid</exception>
    /// <exception cref="System.Exception">Thrown when Twilio API call fails</exception>
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

/// <summary>
/// Interface for WhatsApp messaging services that can send messages to users.
/// Abstracts the underlying messaging implementation (Twilio, Mock, etc.) for testability and flexibility.
/// </summary>
public interface ITwilioMessenger
{
    /// <summary>
    /// Sends a message to a WhatsApp user.
    /// </summary>
    /// <param name="to">The recipient's WhatsApp number (e.g., "whatsapp:+1234567890")</param>
    /// <param name="message">The message content to send</param>
    /// <returns>A task representing the asynchronous send operation</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when the service is not properly configured</exception>
    Task SendMessageAsync(string to, string message);
}