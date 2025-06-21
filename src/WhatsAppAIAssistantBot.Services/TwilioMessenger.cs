namespace WhatsAppAIAssistantBot.Services;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Microsoft.Extensions.Configuration;

public  class TwilioMessenger(IConfiguration configuration) : ITwilioMessenger
{
    public  async Task SendMessageAsync(string to, string message)
    {
        if (configuration == null)
            throw new InvalidOperationException("TwilioMessenger not initialized.");

        var accountSid = configuration["Twilio:AccountSid"] ?? throw new InvalidOperationException("Twilio Account SID is not configured.");
        var authToken = configuration["Twilio:AuthToken"] ?? throw new InvalidOperationException("Twilio Auth Token is not configured.");
        var fromNumber = configuration["Twilio:FromNumber"] ?? throw new InvalidOperationException("Twilio From Number is not configured.");

        TwilioClient.Init(accountSid, authToken);

        await MessageResource.CreateAsync(
            body: message,
            from: new PhoneNumber(fromNumber),
            to: new PhoneNumber($"whatsapp:{to}") 
        );
    }
}

public interface ITwilioMessenger
{
    Task SendMessageAsync(string to, string message);
}